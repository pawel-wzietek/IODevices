
using System;

using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using NationalInstruments.NI4882;


namespace IODevices
{ 

    public class GPIBDevice_NINET : IODevice   //version with boardSRQ: wakeup all devices
    {

        public NationalInstruments.NI4882.Device NIDevice;
        public NationalInstruments.NI4882.Board NIBoard
        {get {return  boards[gpibboard];}
        }
       
        protected byte gpibaddress;  //as in NIDevice

        protected int gpibboard;


        //default
        private const TimeoutValue defaulttimeoutcode = TimeoutValue.T1s;
        private string CrLf = System.Text.Encoding.UTF8.GetString(new byte[2] { 13, 10 }); //CrLf;

        private static Board[] boards;


        //variables and code used by notify
        public static int delaynotify=5;

        private static List<GPIBDevice_NINET> notifylist;//list of devices to notify
        private static object locklist = new object();
        private static GpibStatusFlags notifymask = GpibStatusFlags.ServiceRequest; //srq line


        private bool _enableNotify = false;
        public override bool EnableNotify
        {
            set
            {
                if (!_enableNotify  && value)
                {
                    lock (locklist)
                    {
                    if (notifycount(gpibboard) == 0)
                      { boards[gpibboard].Notify(notifymask, cbnotify, null); }

                        notifylist.Add(this);
                    }
                   
                    _enableNotify = true;

                }
                if (_enableNotify && !value)
                {
                    lock (locklist)
                    {
                        notifylist.Remove(this);
                        if (notifycount(gpibboard) == 0) { boards[gpibboard].Notify(0, cbnotify, null); }
                    }
                    _enableNotify = false; 
                }
            
              }
            get { return _enableNotify; }
        }


        private static int notifycount(int boardnum)
        {
            int count = 0;

                foreach (GPIBDevice_NINET device in notifylist)
                    if (boardnum == device.gpibboard) { count++; }

            return count;
        }

        //notify callback: public delegate void NotifyCallback(Object sender,NotifyData notifyData)
        public static void cbnotify(Object sender, NotifyData notifyData)
        {

            lock (locklist)
            {
                foreach (GPIBDevice_NINET device in notifylist)
                { if (sender.Equals(device.NIBoard)) { device.WakeUp(); } }  //interrupt waiting for next read/poll trial 
            }
            Thread.Sleep(delaynotify); //delay before rearming
            notifyData.SetReenableMask(notifymask); //rearming to allow next notify 

        }



        static GPIBDevice_NINET()//static constructor: define empty board list and notify list
        {
            boards = new Board[10];//max 10 boards?

            for (int i = 0; i <= boards.Length - 1; i++){boards[i] = null;}

            notifylist = new List<GPIBDevice_NINET>();

        }



        //******************
        public int BufferSize
        {
            get { return NIDevice.DefaultBufferSize; }
            set { NIDevice.DefaultBufferSize = value; }
        }
        public GPIBDevice_NINET(string name, string addr)
            : base(name, addr)
        {

            create(name, addr, 32 * 1024);

        }

        public GPIBDevice_NINET(string name, string addr, int defaultbuffersize)
            : base(name, addr)
        {

            create(name, addr, defaultbuffersize);

        }
        //common part of constructor

        private void create(string name, string addr, int defaultbuffersize)
        {

            //try to create device (but don't catch catch exceptions in constructor to avoid creating ill-defined objects)
          

            IODevice.ParseGpibAddr(addr, out gpibboard, out gpibaddress);

            //added in version of Apr 2017:
            if (boards[gpibboard]==null)
            {
                statusmsg = "trying to initialize NI board n°" + gpibboard; 
                boards[gpibboard] = new Board(gpibboard);
               // can add here other board initialization (IFC etc.)   
                boards[gpibboard].UseAutomaticSerialPolling = false;//'apparently necessary for Notify to be reliable
                boards[gpibboard].SynchronizeCallbacks=false; //don't need since the callback is thread-safe
            }

            statusmsg = "trying to create device '" + name + "' at address " + gpibaddress.ToString();
            NIDevice  = new Device(gpibboard, gpibaddress);
            NIDevice.IOTimeout = defaulttimeoutcode;

            NIDevice.DefaultBufferSize = defaultbuffersize;

            //EOI configuration
            NIDevice.SetEndOnWrite = true;
            NIDevice.TerminateReadOnEndOfString = false;


            
            statusmsg = "sending clear to device " + name;
            NIDevice.Clear();

            interfacename = "NINET";
            //modified in 2018:
            interfacelockid = gpibboard;  //each NI board has its own bus lock (driver is thread-safe)
            AddToList();
            //register in device list displayed by DevicesForm (is not called by base class constructor to avoid registering ill-defined devices in case an exception occurs here)
            statusmsg = "";

        }


        protected override void DisposeDevice()
        {

            if (NIDevice != null)
            {
                EnableNotify = false; 
                NIDevice.Dispose();
            }

        }


        protected override int Send(string cmd, ref int errcode, ref string errmsg)
        {
            //send cmd, return 0 if ok, 1 if timeout,  other if other error


            int retval = 0;

            try
            {

                NIDevice.Write(cmd);

                //so status=1 tmo on send;
                // =3 tmo on rcv, =4 other err on send, =6 other err on rcv
            }
            catch (GpibException ex)
            {
                errcode = Convert.ToInt32(ex.ErrorCode);
                errmsg = ex.Message;
                //timeout 
                if (errcode == 6)
                {
                    retval = 1;
                    //send tmo

                }
                else                     //other error
                {
                    retval = 2;

                }

             
            }
            catch (Exception ex)    //curiously some gpib exceptions fall here
            {
                retval = 2;
                errmsg = ex.Message;
                errcode = Convert.ToInt32((new GpibStatus()).ThreadError);
            }

            return retval;
        }


        //--------------------------
        protected override int PollMAV(ref bool mav, ref byte statusbyte, ref int errcode, ref string errmsg)
        {
            //poll for status, return MAV bit 
            //spoll,  return 0 if ok, 1 if timeout,  other if other error

            var retval = 0;

            try
            {

                statusbyte = Convert.ToByte(NIDevice.SerialPoll());
 
                mav = (statusbyte & MAVmask) != 0;

                //status=1 tmo on send,  =3 tmo on rcv, =4 other err on send, =6 other err on rcv
            }
            catch (GpibException ex)
            {
                errcode = Convert.ToInt32(ex.ErrorCode);
                //error 6 "oper aborted" means timeout
                if (ex.ErrorCode == GpibError.IOOperationAborted)
                {

                    retval = 1;
                    errmsg = "serial poll timeout";
                    //ex.Message not always clear ("aborted" etc.)


                   
                }
                else   //other error
                {

                    errmsg = ex.Message;
                    retval = 2;
                }
            }
            catch (Exception ex)
            {
                retval = 2;
                errmsg = ex.Message;
                errcode = Convert.ToInt32((new GpibStatus()).ThreadError);

            }

            return retval;

        }



        //'--------------------
        protected override int ReceiveByteArray(ref byte[] arr, ref bool EOI, ref int errcode, ref string errmsg)
        {


            var retval = 0;

            try
            {

                arr = NIDevice.ReadByteArray();
                GpibStatus st = new GpibStatus();
                EOI = (Convert.ToInt32(st.ThreadStatus) & Convert.ToInt32(GpibStatusFlags.End)) != 0;

                //so status=1 tmo on send;
                // =3 tmo on rcv, =4 other err on send, =6 other err on rcv
            }
            catch (GpibException ex)
            {
                errcode = Convert.ToInt32(ex.ErrorCode);
                errmsg = ex.Message;
                //error 6 "oper aborted" means timeout
                if (ex.ErrorCode == GpibError.IOOperationAborted)
                {
                    retval = 1;
                    //other error
                }
                else
                {
                    retval = 2;
                }
            }
            catch (Exception ex)
            {
                retval = 2;
                errmsg = ex.Message;
                errcode = Convert.ToInt32((new GpibStatus()).ThreadError);

            }

            return retval;

        }


        protected override int ClearDevice(ref int errcode, ref string errmsg)
        {

            int retval = 0;



            try
            {
                NIDevice.Clear();
                retval = 0;
            }
            catch (Exception ex)
            {
                retval = 1;
                errmsg = ex.Message + CrLf + "cannot clear device";
            }

            return retval;
        }




    }
}