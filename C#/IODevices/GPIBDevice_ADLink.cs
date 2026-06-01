
using System;

using System.Data;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace IODevices
{


	//uses gpib-32.dll  from ADLINK (but call syntax compatible with the NI dll of the same name: can use this library for NI boards too)
	//this name conflicts with old versions of dlls by NI, Keithley or MCC 
	// to change the name of the dll change the line:    Private Const _GPIBDll As String = "gpib-32.dll"



	public class GPIBDevice_ADLink : IODevice
	{


		protected byte[] buffer;
		public int BufferSize {
			get { return buffer.Length; }
			set { buffer = new byte[value + 1]; }
		}

        public int IOTimeoutCode
        {
            //see GpibConst
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

        private int _timeoutcode = GpibDll.GpibConst.T3s;  //default value 

   

        //variables and code used by notify
        private static int[] boardud;  //board unit descriptors for all boards
        public static int delaynotify = 5; //delay before rearming

        private static List<GPIBDevice_ADLink> notifylist;          //list of devices to notify
        private static object locklist = new object();
        private static int[] notifymask;     // masks for each board
        private static int userdata;//not used
        private static GpibDll.NotifyCallback cbdelegate;  

        private bool _enableNotify = false;


        public override bool EnableNotify
        {
            set
            {
                if (!_enableNotify && value)
                {

                    lock (locklist)
                    {
                    if (notifycount(gpibboard)==0) //first device on this board
                        {
                        notifymask[gpibboard] = GpibDll.GpibConst.SRQI;
                        GpibDll.ibnotify(boardud[gpibboard], GpibDll.GpibConst.SRQI, cbdelegate, ref userdata);
                        }

                    notifylist.Add(this); 
                    }

                    _enableNotify = true;

                }
                if (_enableNotify && !value)
                {
                    lock (locklist)
                    {
                        notifylist.Remove(this);

                        if (notifycount(gpibboard) == 0)//was last device on this board
                        {
                            notifymask[gpibboard] = 0; //for pending notify events
                            Thread.Sleep(delaynotify);
                            GpibDll.ibnotify(boardud[gpibboard], 0, cbdelegate, ref userdata);
                        }
                    }
                  _enableNotify = false;
                }

            }
            get { return _enableNotify; }
        }


        private static int notifycount(int boardnum)
        {
            int count=0;
             foreach (GPIBDevice_ADLink device in notifylist) 
                    if (boardnum == device.gpibboard) {count++;}
             
            return count;
            }

        //notify callback:  public delegate uint NotifyCallback(int ud, int ibsta, int iberr, int ibcnt, [MarshalAs(UnmanagedType.AsAny)] object RefData);//refdata not used here

        public static int cbnotify(int ud, int ibsta, int iberr, int ibcnt, ref int RefData)//refdata not used here
        {
            int board = 0;
            int retval;

            lock (locklist)
            {
                foreach (GPIBDevice_ADLink device in notifylist)
                {
                    if (ud == boardud[device.gpibboard])
                    {
                        device.WakeUp();   //interrupt waiting for next read/poll trial 
                        board = device.gpibboard; //identify board n°
                    }
                }
                retval = notifymask[board];
            }
            if (retval != 0) { Thread.Sleep(delaynotify); } //delay before rearming

                return retval; //return mask to rearm for next notify (or 0 if disabling : safer if notify disabled while there are pending events)

        }



        //static constructor
		static GPIBDevice_ADLink()
		{const int maxboards=10;

			boardud = new int[maxboards];
            notifymask = new int[maxboards];

			int i = 0;
			for (i = 0; i <= boardud.Length - 1; i++) {
				boardud[i] = 0;
                notifymask[i] = 0; 
			}

            notifylist=new List<GPIBDevice_ADLink>();
            cbdelegate = (GpibDll.NotifyCallback)cbnotify;

		}



		public GPIBDevice_ADLink(string name, string addr) : base(name, addr)
		{
			//init base class storing name and addr
			create(name, addr, 32 * 1024);
		}

		public GPIBDevice_ADLink(string name, string addr, int defaultbuffersize) : base(name, addr)
		{
			//init base class storing name and addr
			create(name, addr, defaultbuffersize);
		}

		//common part of constructor

		private void create(string name, string addr, int defaultbuffersize)
		{
            

		
			IODevice.ParseGpibAddr(addr, out gpibboard, out gpibaddress);

            try
            {
			if (boardud[gpibboard]==0) 
                {
                string boardname = "GPIB" + gpibboard.ToString().Trim();

                statusmsg = "trying to initialize " + boardname;
                int bud = GpibDll.ibfind(boardname);

                if (bud != -1)
                  { boardud[gpibboard] = bud; }
                else
                  {throw new Exception("cannot find GPIB board n°" + gpibboard);}

				//GpibDll.SendIFC(gpibboard);   //some devices don't like it...

                GpibDll.ibconfig(boardud[gpibboard], GpibDll.GpibConst.IbcAUTOPOLL, 0); //disable autopolling

			    }


			//catchinterfaceexceptions = False  'set when debugging read/write routines

			BufferSize = defaultbuffersize;

			interfacelockid = 20;

			interfacename = "ADLink";
			

			//try to create device



            statusmsg = "trying to create device '" + name + "' at address " + addr;
			devid = GpibDll.ibdev(gpibboard, gpibaddress, 0, _timeoutcode, 1, 0);

            int devsta = GpibDll.ThreadIbsta();
            if ( (devsta & GpibDll.GpibConst.EERR) !=0) 
                     throw new Exception("cannot get device descriptor on board " + gpibboard);


             statusmsg = "sending clear to device " + name;
             GpibDll.ibclr(devid);

				

			//in this dll may happen when USB-GPIB board not connected!!! (however sendIFC has no problem!
			} catch (System.AccessViolationException) {

				throw new Exception("exception thrown when trying to create device: GPIB board not connected?");

			}


			//EOI configuration

			int sta = 0;
			sta = GpibDll.ibconfig(devid, GpibDll.GpibConst.IbcEOSwrt, 0);
			sta = GpibDll.ibconfig(devid, GpibDll.GpibConst.IbcEOT, 1);


			AddToList();
			statusmsg = "";
		}

		protected override void DisposeDevice()
		{
            if (devid != 0) 
               {
                EnableNotify = false;
                GpibDll.ibonl(devid, 0); 
            }

		}


		protected override int Send(string cmd, ref int errcode, ref string errmsg)
		{
			//send cmd, return 0 if ok, 1 if timeout,  other if other error

			int retval = 0;
			int sta = 0;
			bool err = false;
			bool tmo = false;

			try {
				retval = 0;

				sta = GpibDll.ibwrt(devid, cmd, cmd.Length);


				err = (sta & GpibDll.GpibConst.EERR)!=0;

				if (err) {
					errcode = GpibDll.ThreadIberr();
                    tmo = (errcode == GpibDll.GpibConst.EABO);
					if (tmo) {
						retval = 1;
						errmsg = " write timeout";
					} else {
						retval = 2;

						string s = GpibDll.GpibConst.errmsg(errcode);
						if (!string.IsNullOrEmpty(s)) {
							errmsg = " error in 'send':" + s;
						} else {
							errmsg = " error in 'send'";
						}
					}
				}


			//? not needed
			} catch (Exception ex) {
				err = true;
				retval = 2;
				errmsg = "exception in ibwrt:\\n" + ex.Message;
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
			bool err = false;
			bool tmo = false;


			//reading
			try {

				retval = 0;

				sta = GpibDll.ibrsp(devid, ref statusbyte);


				err = (sta & GpibDll.GpibConst.EERR)!=0;
				mav = (statusbyte & MAVmask)!=0;
				//SerialPollFlags.MessageAvailable


				//status=1 tmo on send,  =3 tmo on rcv, =4 other err on send, =6 other err on rcv


				if (err) {
					errcode = GpibDll.ThreadIberr();
                    tmo = (errcode == GpibDll.GpibConst.EABO);
					if (tmo) {
						retval = 1;
						errmsg = "serial poll timeout";
					} else {
						retval = 2;
						string s = GpibDll.GpibConst.errmsg(errcode);
						if (!string.IsNullOrEmpty(s)) {
							errmsg = "serial poll error:" + s;
						} else {
							errmsg = "serial poll error";
						}

					}
				}


			} catch (Exception ex) {
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



			//reading
			try {


				cnt = buffer.Length;
				sta = GpibDll.ibrd(devid, buffer, cnt);


				err = (sta & GpibDll.GpibConst.EERR)!=0;
				if (err) {
					errcode = GpibDll.ThreadIberr();
					if (errcode == GpibDll.GpibConst.EABO) {
						retval = 1;
						errmsg = "receive timeout";
					} else {
						retval = 2;
						string s = GpibDll.GpibConst.errmsg(errcode);
						if (!string.IsNullOrEmpty(s)) {
							errmsg = s;
						} else {
							errmsg = "error in 'receive' ";
						}
					}

				} else {

					cnt = GpibDll.ThreadIbcnt();
					arr = new byte[cnt];
					Array.Copy(buffer, arr, cnt);

					EOI = (sta & GpibDll.GpibConst.EEND)!=0;
					retval = 0;
				}

			} catch (Exception ex) {
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


			try {
				sta = GpibDll.ibclr(devid);


				err = (sta & GpibDll.GpibConst.EERR)!=0;

				if (err) {
					errcode = GpibDll.ThreadIberr();
					retval = 1;
					string s = GpibDll.GpibConst.errmsg(errcode);
					if (!string.IsNullOrEmpty(s)) {
						errmsg = "error in 'cleardevice': " + s;
					} else {
						errmsg = "error in 'cleardevice' ";
					}
				} else {
					retval = 0;
				}
			} catch (Exception ex) {
				retval = 1;
				errmsg = ex.Message + "\n cannot clear device ";
			}

			return retval;
		}


		//********************************************************************

		// dll import functions
		internal class GpibDll
		{

			private const string _GPIBDll = "gpib-32.dll";


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

					//Timeout
				public const int EABO = 6;
					// Board must be CIC for this function
				public const int ECIC = 1;
					// no listeners 
				public const int ENOL = 2;
					// Board not addressed correctly
				public const int EADR = 3;
					// Invalid board specified
				public const int ENEB = 7;
					// Command error on bus
				public const int EBUS = 14;

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
				//some  ibconfig() options
				public const int IbcPAD = 0x1;
				public const int IbcSAD = 0x2;
				public const int IbcTMO = 0x3;
				public const int IbcEOT = 0x4;
				public const int IbcPPC = 0x5;

				public const int IbcEOSrd = 0xc;
				public const int IbcEOSwrt = 0xd;
				public const int IbcEOScmp = 0xe;
				public const int IbcEOSchar = 0xf;
                public const int IbcAUTOPOLL = 0x0007;

                public const int SRQI = 0x1000;   //mask for SRQ board level notify

				public static string errmsg(int errno)
				{
					string s = "";

					switch (errno) {
						//most common errors
						case ECIC:
							s = "Board is not CIC";
							break;
						case ENOL:
							s = "no listeners";
							break;
						case ENEB:
							s = "Invalid board specified";
							break;
						case EADR:
							s = "Board not addressed correctly";
							break;
						case EBUS:
							s = "Command error on bus";

							break;
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


            [DllImport(_GPIBDll, EntryPoint = "ibfind")]
            private static extern int _ibfind( [MarshalAs(UnmanagedType.LPStr)] string name);
            protected static internal int ibfind(string name)
            {
                return _ibfind( name);
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

			[DllImport(_GPIBDll, EntryPoint = "ibwrt")]
			private static extern int _ibwrt(int ud, [MarshalAs(UnmanagedType.LPStr)] string buf, int count);
			protected static internal int ibwrt(int ud, string buf, int count)
			{
				return _ibwrt(ud, buf, count);
			}
			[DllImport(_GPIBDll, EntryPoint = "ibrd")]
			private static extern int _ibrd(int ud, [MarshalAs(UnmanagedType.LPArray),Out] byte[] buffer, int count);

			protected static internal int ibrd(int ud, byte[] buffer, int count)
			{

				return _ibrd(ud, buffer, count);
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


            // notify  event handler functions

            // C prototype for callback handler:
           //int __stdcall Callback (int ud,int ibsta,int iberr,long ibcntl,void * RefData)
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate int NotifyCallback(int ud, int ibsta, int iberr, int ibcnt, ref int RefData);//refdata not used here



            //uint ibnotify (int ud,int mask,GpibNotifyCallback_t Callback,void * RefData)
            [DllImport(_GPIBDll, EntryPoint = "ibnotify")]
            public static extern uint ibnotify(int ud, int mask, [MarshalAs(UnmanagedType.FunctionPtr)] NotifyCallback callback,
                                          ref int RefData);
          
            
        

		}


	}

}
