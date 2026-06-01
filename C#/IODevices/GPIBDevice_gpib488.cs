
using System;

using System.Data;
using System.Diagnostics;


using System.Text;
using System.Runtime.InteropServices;


namespace IODevices
{




    //uses gpib488.dll  used by  MCC and Keithley boards

    // to change the name of the dll change the line:   private const string _GPIBDll = "gpib488.dll";




    public class GPIBDevice_gpib488 : IODevice
    {


        protected byte[] buffer;
        public int BufferSize
        {
            get { return buffer.Length; }
            set { buffer = new byte[value]; }
        }

        public int IOTimeoutCode    //see GpibConst 
        {
           
            get { return _timeoutcode; }
            set
            {
                _timeoutcode = value;
                GpibDll.ibconfig(devid, GpibDll.GpibConst.IbcTMO, value);
            }
        }
        protected byte gpibaddress;

        protected int gpibboard;
        //device id as returned by ibdev
        protected int devid=0;


        private static bool[] boardinitialized;

        private int _timeoutcode = GpibDll.GpibConst.T3s;

        static GPIBDevice_gpib488()//static constructor
        {
            boardinitialized = new bool[4];//max 4 boards?

            int i = 0;
            for (i = 0; i <= boardinitialized.Length - 1; i++)
            {
                boardinitialized[i] = false;
            }

        }

        public GPIBDevice_gpib488(string name, string addr)
            : base(name, addr)
        {
            create(name, addr, 32 * 1024);
        }


        public GPIBDevice_gpib488(string name, string addr, int defaultbuffersize)
            : base(name, addr)
        {
            create(name, addr, defaultbuffersize);
        }

        protected void create(string name, string addr, int defaultbuffersize)
        {
            //init base class storing name and addr

            statusmsg = "trying to create device '" + name + "' at address " + addr;
            IODevice.ParseGpibAddr(addr, out gpibboard, out gpibaddress);


            if (!boardinitialized[gpibboard])
            {
                GpibDll.SendIFC(gpibboard);
                boardinitialized[gpibboard] = true;
            }


            //catchinterfaceexceptions = False  'set when debugging read/write routines

            BufferSize = 32 * 1024;

            interfacelockid = 21;
            interfacename = "gpib488";
            statusmsg = "";

            //try to create device



            try
            {
                devid = GpibDll.ibdev(gpibboard, gpibaddress, 0, _timeoutcode, 1, 0);

                int devsta = GpibDll.ThreadIbsta();
                if ((devsta & GpibDll.GpibConst.EERR) != 0)
                    throw new Exception("cannot get device descriptor on board " + gpibboard);


                statusmsg = "sending clear to device " + name;
                GpibDll.ibclr(devid);



                //in this dll happens when USB-GPIB board not connected!!! 
            }
            catch (System.AccessViolationException)
            {

                throw new Exception("exception thrown when trying to create device: GPIB board not connected?");

            }

            //EOI configuration



            GpibDll.ibconfig(devid, GpibDll.GpibConst.IbcEOSwrt, 0);
            GpibDll.ibconfig(devid, GpibDll.GpibConst.IbcEOT, 1);

            AddToList();
            statusmsg = "";
        }

        protected override void DisposeDevice()
        {
            if (devid != 0) { GpibDll.ibonl(devid, 0); }

        }


        protected override int Send(string cmd, ref int errcode, ref string errmsg)
        {
            //send cmd, return 0 if ok, 1 if timeout,  other if other error

            int retval = 0;
            int sta = 0;
            //int cnt = 0;
            bool err = false;
            bool tmo = false;

            try
            {
                retval = 0;

                sta = GpibDll.ibwrt(devid, cmd, cmd.Length);


                err = (sta & GpibDll.GpibConst.EERR) != 0;

                if (err)
                {
                    errcode = GpibDll.ThreadIberr();
                    tmo = (errcode == GpibDll.GpibConst.EABO);
                    if (tmo)
                    {
                        retval = 1;
                        errmsg = " write timeout";
                    }
                    else
                    {
                        retval = 2;
                        string s = GpibDll.GpibConst.errmsg(errcode);
                        if (s != "")
                        { errmsg = "error in 'send':" + s; }
                        else
                        { errmsg = "error in 'send' "; }
                    }
                }


                //? not needed
            }
            catch (Exception ex)
            {
                err = true;
                retval = 2;
                errmsg = ex.Message;
            }


            return retval;
        }


        //--------------------------
        protected override int PollMAV(ref bool mav, ref byte statusbyte, ref int errcode, ref string errmsg)
        {
            //poll for status, return MAV bit 
            //spoll,  return 0 if ok, 1 if timeout,  other if other error


            int retval = 0;
            int sta = 0;
            //	int cnt = 0;

            bool err = false;
            bool tmo = false;


            //reading
            try
            {

                retval = 0;
              
                sta = GpibDll.ibrsp(devid, ref statusbyte);


                err = (sta & GpibDll.GpibConst.EERR) != 0;
                mav = (statusbyte & MAVmask) != 0;



                //status=1 tmo on send,  =3 tmo on rcv, =4 other err on send, =6 other err on rcv


                if (err)
                {
                    errcode = GpibDll.ThreadIberr();
                    tmo = (errcode == GpibDll.GpibConst.EABO);
                    if (tmo)
                    {
                        retval = 1;
                        errmsg = "serial poll timeout";
                    }
                    else
                    {
                        retval = 2;
                        string s = GpibDll.GpibConst.errmsg(errcode);
                        if (s != "")
                        { errmsg = "serial poll error:" + s; }
                        else
                        { errmsg = "serial poll error"; }
                    }
                }


            }
            catch (Exception ex)
            {
                retval = 2;
                errmsg = ex.Message;

            }

            return retval;

        }




        //'--------------------
        protected override int ReceiveByteArray(ref byte[] arr, ref bool EOI, ref int errcode, ref string errmsg)
        {


            int retval = 0;

            bool err = false;
            int sta = 0;
            int cnt = 0;

            bool tmo;

            //reading
            try
            {


                cnt = buffer.Length;
                sta = GpibDll.ibrd(devid, buffer, cnt);


                err = (sta & GpibDll.GpibConst.EERR) != 0;
                if (err)
                {
                    errcode = GpibDll.ThreadIberr();
                    tmo = (errcode == GpibDll.GpibConst.EABO);
                    if (tmo)
                    {
                        retval = 1;
                        errmsg = "receive timeout";
                    }
                    else
                    {
                        retval = 2;
                        string s = GpibDll.GpibConst.errmsg(errcode);
                        if (s != "")
                        { errmsg = "error in 'receive':" + s; }
                        else
                        { errmsg = "error in 'receive' "; }
                    }

                }
                else
                {


                    cnt = GpibDll.ThreadIbcnt();
                    arr = new byte[cnt];
                    Array.Copy(buffer, arr, cnt);

                    EOI = (sta & GpibDll.GpibConst.EEND) != 0;
                    retval = 0;
                }

            }
            catch (Exception ex)
            {
                retval = 2;
                errmsg = ex.Message;


            }

            return retval;


        }


        protected override int ClearDevice(ref int errcode, ref string errmsg)
        {

             int retval = 0;
   

            bool err = false;
            int sta = 0;
           

            try
            {

                sta = GpibDll.ibclr(devid);
                err = (sta & GpibDll.GpibConst.EERR) != 0; ;

                if (err)
                {
                    errcode = GpibDll.ThreadIberr();
                    retval = 1;
                    string s = GpibDll.GpibConst.errmsg(errcode);
                    if (s != "")
                    { errmsg = "error in 'cleardevice':" + s; }
                    else
                    { errmsg = "error in 'cleardevice' "; }
                }
                else
                {
                    retval = 0;
                }
            }
            catch (Exception ex)
            {
                retval = 1;
                errmsg = ex.Message + "\n cannot clear device ";
            }

            return retval;
        }




        //********************************************************************

        // dll import functions
        internal class GpibDll
        {


            private const string _GPIBDll = "gpib488.dll";


            protected internal class GpibConst
            {
                //status constants
                // Error detected
                public const uint EERR = 0x8000;
                // 
                public const uint TIMO = 0x4000;
                // EOI or EOS detected
                public const uint EEND = 0x2000;

                //some errors

                public const int EABO = 6;  //Timeout

                public const int ENOL = 2; // no listeners 
                public const int ECIC = 1;   // Board must be CIC for this function
                public const int ENEB = 7; //    Invalid board specified

                //timeout option
                public const int T10ms = 7;
                public const int T30ms = 8;
                public const int T100ms = 9;
                public const int T300ms = 10;
                public const int T1s = 11;
                public const int T3s = 12;
                public const int T10s = 13;
                //eot options
                public const int NULLend = 0x0;
                public const int NLend = 0x1;

                public const int DABend = 0x2;
                // some ibconfig() options
                public const int IbcPAD = 0x1;
                public const int IbcSAD = 0x2;
                public const int IbcTMO = 0x3;
                public const int IbcEOT = 0x4;
                public const int IbcPPC = 0x5;
         
                public const int IbcEOSrd = 0xc;
                public const int IbcEOSwrt = 0xd;
                public const int IbcEOScmp = 0xe;
                public const int IbcEOSchar = 0xf;
                public static string errmsg(int errno)
                {
                    string s = "";

                    switch (errno) //most common errors
                    {
                        case ECIC: s = "Board is not CIC"; break;
                        case ENOL: s = "no listeners"; break;
                        case ENEB: s = "Invalid board specified"; break;

                    }
                    return s;
                }
         
            }



            [DllImport(_GPIBDll, EntryPoint = "SendIFC")]
            private static extern void _SendIFC(int board);
            protected static internal void SendIFC(int board)
            {
                _SendIFC(board);
            }

            [DllImport(_GPIBDll, EntryPoint = "ibdev")]
            private static extern int _ibdev(int ubrd, int pad, int sad, int tmo, int eot, int eos);
            protected static internal int ibdev(int board, int pad, int sad, int tmo, int eot, int eos)
            {
                return _ibdev(board, pad, sad, tmo, eot, eos);
            }

            [DllImport(_GPIBDll, EntryPoint = "ibonl")]
            private static extern uint _ibonl(int ud, int v);
            protected static internal uint ibonl(int ud, int v)
            {
                return _ibonl(ud, v);
            }

            [DllImport(_GPIBDll, EntryPoint = "ibconfig")]
            private static extern int _ibconfig(int ud, int opt, int v);
            protected static internal int ibconfig(int ud, int opt, int v)
            {
                return _ibconfig(ud, opt, v);
            }

            //here the IntPtr function is used to detect and adapt to the 32 vs 64 bit environment
            [DllImport(_GPIBDll, EntryPoint = "ibwrt")]
            private static extern int _ibwrt(int ud, [MarshalAs(UnmanagedType.LPStr)]
		                                 string buf, IntPtr count);


            protected static internal int ibwrt(int ud, string buf, int count)
            {
                return _ibwrt(ud, buf, new IntPtr(count));
            }



            [DllImport(_GPIBDll, EntryPoint = "ibrd")]
            private static extern int _ibrd(int ud, [MarshalAs(UnmanagedType.LPArray),Out]
byte[] buffer, IntPtr count);


            protected static internal int ibrd(int ud, byte[] buffer, int count)
            {

                return _ibrd(ud, buffer, new IntPtr(count));
            }


            [DllImport(_GPIBDll, EntryPoint = "ibclr")]
            private static extern int _ibclr(int ud);
            protected static internal int ibclr(int ud)
            {
                return _ibclr(ud);
            }


            [DllImport(_GPIBDll, EntryPoint = "ibrsp")]
            private static extern int _ibrsp(int ud, ref byte spr);
            protected static internal int ibrsp(int ud, ref byte spr)
            {
                return _ibrsp(ud, ref spr);
            }

            [DllImport(_GPIBDll, EntryPoint = "ThreadIbsta")]
            private static extern int _ThreadIbsta();
            protected static internal int ThreadIbsta()
            {
                return _ThreadIbsta();
            }

            [DllImport(_GPIBDll, EntryPoint = "ThreadIberr")]
            private static extern int _ThreadIberr();
            protected static internal int ThreadIberr()
            {
                return _ThreadIberr();
            }

            [DllImport(_GPIBDll, EntryPoint = "ThreadIbcnt")]
            private static extern int _ThreadIbcnt();
            protected static internal int ThreadIbcnt()
            {
                return _ThreadIbcnt();
            }




        }


    }
}