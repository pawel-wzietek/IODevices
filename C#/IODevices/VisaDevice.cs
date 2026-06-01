
using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;


namespace IODevices
{

    //uses "visa32.dll"  which exists in 32bit (windows/syswow64)  and 64 bit((windows/system32) versions
    //dll name defined in the line
    //         private const string _VisaDll = "visa32.dll";   

    public class VisaDevice : IODevice
    {


        
        public int BufferSize
        {
            get { return buffer.Length; }
            //should not be set during operation

            set { buffer = new byte[value]; }
        }


        public uint IOTimeout
        {
            //see GpibConst
            get { 
                uint timeout;
                 GetAttribute(VI_ATTR_TMO_VALUE, out timeout);
                 return timeout; 
                 }
            set
            {
                uint timeout =  value;
                SetAttribute(VI_ATTR_TMO_VALUE, timeout);
            }
        }

        //other functions specific to visa : attributes

        public uint SetAttribute(uint attribute, int attrState)
        {
            return VisaDll.viSetAttribute_Int32(devid, attribute, attrState);
        }
        public uint SetAttribute(uint attribute, uint attrState)
        {
            return VisaDll.viSetAttribute_UInt32(devid, attribute, attrState);
        }



        public uint GetAttribute(uint attribute, out int attrState)
        {
            return VisaDll.viGetAttribute_Int32(devid, attribute, out attrState);
        }
        public uint GetAttribute(uint attribute, out uint attrState)
        {
            return VisaDll.viGetAttribute_UInt32(devid, attribute, out attrState);
        }


        //private fields

        private byte[] buffer;
        private const bool clearonstartup = true; //if send clear when creating device
        protected static int session = 0;  //resource manager session, common for all devices

        protected int devid = 0;

        //Visa constants
        private const uint VI_errmask = 0x1111u;
       

        private const uint VI_ERROR = 0xbfff0000u;
        private const uint VI_ERROR_TMO = 0xbfff0015u;
        private const uint VI_ERROR_NLISTENERS = 0xbfff005fu;
        private const uint VI_ERROR_RSRC_NFOUND = 0xBFFF0011u;

        private const uint VI_SUCCESS_MAX_CNT = 0x3fff0006u;// when EOI not set
          // variables and constants related to notify
         private const uint VI_EVENT_SERVICE_REQ = 0x3FFF200BU;
         private const int VI_HNDLR = 2;
         private int userhandle=0;
         private VisaDll.NotifyCallback cbdelegate;  //unmanaged callback delegate

         private const uint VI_ATTR_TMO_VALUE = 0x3FFF001AU;
        // private string CrLf = "\r\n"; //"System.Text.Encoding.UTF8.GetString(new byte[2] { 13, 10 }); //CrLf;

  
        private bool _enableNotify = false;
        public override bool EnableNotify
        {
            set
            {
                if (!_enableNotify && value)
                {
        
                    //in Visa each device has to install its own handler
                    cbdelegate = (VisaDll.NotifyCallback)cbnotify;

                    VisaDll.viInstallHandler(devid, VI_EVENT_SERVICE_REQ, cbdelegate, ref userhandle);
                    
                    VisaDll.viEnableEvent(devid, VI_EVENT_SERVICE_REQ, VI_HNDLR, 0);

                    _enableNotify = true;

                }
                if (_enableNotify && !value)
                {            
                    VisaDll.viDisableEvent(devid, VI_EVENT_SERVICE_REQ, VI_HNDLR); 

                    VisaDll.viUninstallHandler(devid, VI_EVENT_SERVICE_REQ, cbdelegate, ref userhandle);

                    _enableNotify = false;
                }

            }
            get { return _enableNotify; }
        }

        //handler for Notify

        // C prototype:
        //ViStatus _VI_FUNCH viEventHandler(ViSession vi, ViEventType eventType, ViEvent context, ViAddr userHandle)
        // _VI_FUNCH :  __stdcall in 32 , __fastcall in 64

        private uint cbnotify(int vid, uint eventType, uint context, ref int userHandle)
        {
            if (vid==devid && eventType == VI_EVENT_SERVICE_REQ) { WakeUp();}
            Thread.Sleep(1); //yield before rearming          

            return 0;

        }

        //constructor
        public VisaDevice(string name, string addr)
            : base(name, addr)
        {
            create(name, addr, 32 * 1024);
        }



        public VisaDevice(string name, string addr, int defaultbuffersize)
            : base(name, addr)
        {
            create(name, addr, defaultbuffersize);
        }


        //common part of constructor

        private void create(string name, string addr, int defaultbuffersize)
        {

            int accessmode = 0;
            int opentimeout = 300;

            uint result = 0;


            //open a session

            if (session == 0)
            {
                statusmsg = "opening Visa session...";

                result = VisaDll.viopenDefaultRM(ref session);
                if (result >= VI_ERROR)
                { throw new Exception("cannot open Visa session, code " + result.ToString("X")); }
            }


            //try to create device

            statusmsg = "trying to create device '" + name + "' at address " + addr;

            result = VisaDll.viOpen(session, addr, accessmode, opentimeout, ref devid);

            if (result >= VI_ERROR)
            {
                string msg = "could not open Visa device at address " + addr;

            if ((result == VI_ERROR_NLISTENERS) || (result == VI_ERROR_RSRC_NFOUND)) 
            { msg += "\nno devices detected at this adress";}
                
             msg += "\nerror code: " + result.ToString("X");

                throw new System.Exception(msg);
            }


            if (clearonstartup)
            {
                statusmsg = "sending clear to device " + name;
                result = VisaDll.viClear(devid);


                if (result >= VI_ERROR)
                {
                    string msg = "could not clear Visa device at address " + addr;
                    if (result == VI_ERROR_NLISTENERS)
                    {
                        msg += "\nno listeners detected at this adress";
                    }
                    else
                    {
                        msg += "\nerror code " + result.ToString("X");
                    }
                    throw new Exception(msg);
                }
            } 


            //catchinterfaceexceptions = False  'set when debugging read/write routines

            BufferSize = defaultbuffersize;

            //interfacelockid modified in 2018:
            if (addr.ToUpper().Contains("GPIB"))
            {
                int gpibboard; byte  gpibaddr;
                IODevice.ParseGpibAddr(addr, out gpibboard, out gpibaddr);
                interfacelockid = gpibboard+10;  
            }
            else
            { interfacelockid = -1; }  //no interface lock for non-gpib interfaces
          
            
            
            interfacename = "Visa";
            statusmsg = "";
            AddToList();

            
        }


        



        protected override void DisposeDevice()
        {

            //release unmanaged resources
            EnableNotify = false;
            if (devid != 0) { VisaDll.viClose(devid); devid = 0; }

        

        }


        //finalizer: now implemented in the base class
         //~VisaDevice()
       


        protected override int Send(string cmd, ref int errcode, ref string errmsg)
        {
            //send cmd, return 0 if ok, 1 if timeout,  other if other error

            int retval = 0;

            bool err = false;
            bool tmo = false;
            uint resultwrite = 0;
            int retcount = 0;

            retval = 0;

            resultwrite = VisaDll.viWrite(devid, cmd, ref retcount);

            err = (resultwrite >= VI_ERROR) | (cmd.Length != retcount);
            tmo = (resultwrite == VI_ERROR_TMO);

            if (err)
            {
                errcode = Convert.ToInt32(resultwrite & VI_errmask);
                if (tmo)
                {
                    retval = 1;
                    errmsg = " write timeout";
                }
                else
                {
                    retval = 2;
                    errmsg = " error in 'viWrite', ";
                    if (resultwrite == VI_ERROR_NLISTENERS)
                    {
                        errmsg += "no listeners detected at this adress";
                    }
                    else
                    {
                        errmsg += "code " + resultwrite.ToString("X");
                    }
                   
                }
            }

            return retval;
        }


        //--------------------------
        protected override int PollMAV(ref bool mav, ref byte statusbyte, ref int errcode, ref string errmsg)
        {
            //poll for status, return MAV bit 
            //spoll,  return 0 if ok, 1 if timeout,  other if other error

            int retval = 0;
            uint result = 0;
            bool err = false;
            bool tmo = false;
            short stb = 0;


            retval = 0;
            result = VisaDll.viReadSTB(devid, ref stb);
            statusbyte = Convert.ToByte(stb & 255);
            mav = (statusbyte & MAVmask) != 0; //SerialPollFlags.MessageAvailable=16

            err = (result > VI_ERROR);
            tmo = (result == VI_ERROR_TMO);
            
            //status=1 tmo on send,  =3 tmo on rcv, =4 other err on send, =6 other err on rcv


            if (err)
            {
                errcode = Convert.ToInt32(result & VI_errmask);

                if (tmo)
                {
                    retval = 1;
                    errmsg = "serial poll timeout";
                }
                else
                {
                    retval = 2;
                    errmsg = "serial poll error, code: " + result.ToString("X");
                }
            }

            return retval;

        }

        //'--------------------
        protected override int ReceiveByteArray(ref byte[] arr, ref bool EOI, ref int errcode, ref string errmsg)
        {

            int retval = 0;

            bool err = false;
            uint result = 0;

            int cnt = 0;
            bool tmo = false;

            retval = 0;
            result = VisaDll.viRead(devid, buffer, buffer.Length, ref cnt);

            err = !(result == 0 | result == VI_SUCCESS_MAX_CNT);

            EOI = result == 0;

            tmo = (result == VI_ERROR_TMO);

            if (err)
            {
                errcode = Convert.ToInt32(result & VI_errmask);

                if (tmo)
                {
                    retval = 1;
                    errmsg = " read timeout";
                }
                else
                {
                    retval = 2;
                    errmsg = " error in 'viRead', code: " + result.ToString("X");
                }
            }
            else
            {
                arr = new byte[cnt];
                Array.Copy(buffer, arr, cnt);
            }
            return retval;
        }


        protected override int ClearDevice(ref int errcode, ref string errmsg)
        {
            int retval = 0;
            uint result = 0;


            retval = 0;


            result = VisaDll.viClear(devid);


            if (result != 0)
            {
                retval = 1;
                errcode = Convert.ToInt32(result & VI_errmask);
                errmsg = "error in viClear, code: " + result.ToString("X");
            }

            return retval;
        }

       

    }


    //********************************************************************

    // dll import functions
    class VisaDll
    {


        private const string _VisaDll = "visa32.dll";

        [DllImport(_VisaDll, EntryPoint = "viOpenDefaultRM")]
        private static extern uint _viopenDefaultRM(ref int sesn);
        public static uint viopenDefaultRM(ref int sesn)
        {
            return _viopenDefaultRM(ref sesn);
        }

        [DllImport(_VisaDll, EntryPoint = "viOpen")]
        private static extern uint _viopen(int sesn, [MarshalAs(UnmanagedType.LPStr)]
string rsrcName, int accessMode, int openTimeout, ref int v);
        public static uint viOpen(int sesn, string rsrcName, int accessMode, int openTimeout, ref int v)
        {

            return _viopen(sesn, rsrcName, accessMode, openTimeout, ref v);

        }
        [DllImport(_VisaDll, EntryPoint = "viWrite")]
        private static extern uint _viWrite(int vi, [MarshalAs(UnmanagedType.LPStr)]
string buf, int maxcount, ref int retcount);
        public static uint viWrite(int vi, string cmd, ref int retcount)
        {

            return _viWrite(vi, cmd, cmd.Length, ref retcount);


        }


        [DllImport(_VisaDll, EntryPoint = "viRead")]
        private static extern uint _viRead(int vi, [MarshalAs(UnmanagedType.LPArray),Out]
byte[] buf, int maxcount, ref int retcount);
        public static uint viRead(int vi, byte[] buf, int maxcount, ref int cnt)
        {


            return _viRead(vi, buf, maxcount, ref cnt);

        }

        [DllImport(_VisaDll, EntryPoint = "viClear")]
        private static extern uint _viClear(int vid);
        public static uint viClear(int vid)
        {
            return _viClear(vid);
        }


        [DllImport(_VisaDll, EntryPoint = "viReadSTB")]
        private static extern uint _viReadSTB(int vid, ref short stb);
        public static uint viReadSTB(int vid, ref short stb)
        {
            return _viReadSTB(vid, ref stb);
        }


        [DllImport(_VisaDll, EntryPoint = "viClose")]
        private static extern uint _viClose(int vid);
        public static uint viClose(int vid)
        {
            return _viClose(vid);
        }

        //set/get attribute defined for most common attribute types: Int32,UInt32 (may be extended if needed for other types)

        [DllImport(_VisaDll, EntryPoint = "viSetAttribute")]
        internal static extern uint viSetAttribute_Int32(int vid, uint attribute, int attrState);


        [DllImport(_VisaDll, EntryPoint = "viSetAttribute")]
        internal static extern uint viSetAttribute_UInt32(int vid, uint attribute, uint attrState);


        [DllImport(_VisaDll, EntryPoint = "viGetAttribute")]
        internal static extern uint viGetAttribute_Int32(int vid, uint attribute, out int attrState);
        
        [DllImport(_VisaDll, EntryPoint = "viGetAttribute")]
        internal static extern uint viGetAttribute_UInt32(int vid, uint attribute, out uint attrState);
       

  //  event handler functions
        
        // C prototype for callback handler:
        //ViStatus _VI_FUNCH viEventHandler(ViSession vi, ViEventType eventType, ViEvent context, ViAddr userHandle)
        // _VI_FUNCH :  __stdcall in 32 , __fastcall in 64
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint NotifyCallback(int vid, uint eventType, uint context, ref int userHandle);//userHandle not used here


        //ViStatus viInstallHandler(ViSession vi, ViEventType eventType, ViHndlr handler, ViAddr userHandle)
        [DllImport(_VisaDll, EntryPoint = "viInstallHandler")]
        public static extern uint viInstallHandler(int vid, uint eventType, 
                            [MarshalAs(UnmanagedType.FunctionPtr)] NotifyCallback callback, 
                                ref int userHandle);

      //  ViStatus viUninstallHandler(ViSession vi, ViEventType eventType, ViHndlr handler, ViAddr userHandle)

        [DllImport(_VisaDll, EntryPoint = "viUninstallHandler")]
        public static extern uint viUninstallHandler(int vid, uint eventType,
                            [MarshalAs(UnmanagedType.FunctionPtr)] NotifyCallback callback,
                                ref int userHandle);

        //ViStatus viEnableEvent(ViSession vi, ViEventType eventType, ViUInt16 mechanism, ViEventFilter context)


        [DllImport(_VisaDll, EntryPoint = "viEnableEvent")]
        public static extern uint viEnableEvent(int vid, uint eventType, UInt16 mechanism, uint context);

        //ViStatus viDisableEvent(ViSession vi, ViEventType eventType, ViUInt16 mechanism)
        [DllImport(_VisaDll, EntryPoint = "viDisableEvent")]
        public static extern uint viDisableEvent(int vid, uint eventType, UInt16 mechanism); 

    }

}