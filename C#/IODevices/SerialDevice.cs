using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.IO.Ports;


namespace IODevices
{

    public class SerialDevice : IODevice
    {

        protected int defaultreadtimeout = 5;  //port read timeout should be set to short value 
        protected int defaultwritetimeout=1000;
       

        protected SerialPort commport = null;
        protected string commerr;

        protected string portname;

        protected string fullportspec;

        private static string CrLf = "\r\n"; //System.Text.Encoding.UTF8.GetString(new byte[2] { 13, 10 }); //CrLf;
        private static string Cr = "\r"; // System.Text.Encoding.UTF8.GetString(new byte[1] { 13}); 
        private static string Lf = "\n"; // System.Text.Encoding.UTF8.GetString(new byte[1] { 10 }); 


        private bool _enableDataReceivedEvent = false;
        public bool EnableDataReceivedEvent
        {
            set
            {
                if (!_enableDataReceivedEvent && value)
                {
                    //use events to quit waiting delays, then delays can be set long
                    delayread = 100;
                    delayrereadontimeout = 100;
                    commport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);


                    _enableDataReceivedEvent = true;

                }
                if (_enableDataReceivedEvent && !value)
                {
                    delayread = 10;
                    delayrereadontimeout = 20;
                    commport.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                    _enableDataReceivedEvent = false;
                }

            }
            get { return _enableDataReceivedEvent; }
        }


        

        public SerialDevice(string name, string addr)
            : base(name, addr)
        {

            init(name, addr, "", 4096);

        }

        //more complete constructor

        public SerialDevice(string name, string addr, string termstr, int buffersize)
            : base(name, addr)
        {
            //init base class storing name and addr


            init(name, addr, termstr, buffersize);
        }

        private void init(string name, string addr, string termstr, int buffersize)
        {
            //termstr defines the "end of line" character or sequence of caracters (eg. Cr, Lf, CRLF etc),  will override the setting in "addr"

            fullportspec = addr;


            interfacelockid = -1;
            //no bus lock (serial ports perfectly thread-safe)

            statusmsg = "opening comm port for device " + name;
            OpenComm(fullportspec, buffersize);

            if (!string.IsNullOrEmpty(termstr))
                commport.NewLine = termstr;
            //override settings in addr if any

            devaddr = portname;
            //store short addr for display  (baud etc removed)
            interfacename = "serial";

           EnableDataReceivedEvent = true;

            statusmsg = "";
            AddToList();          
        }


        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            WakeUp();            
        }

        protected override void DisposeDevice()
        {
            try
            {
                commport.Close();
            }
            catch
            {
            }

        }

        protected override int Send(string cmd, ref int errcode, ref string errmsg)
        {
            //send cmd, return 0 if ok, 1 if timeout,  other if other error



            commport.DiscardInBuffer();  


            var retval = 0;


            try
            {
                commport.WriteLine(cmd);

            }
            catch (TimeoutException )
            {
                errcode = 1;
                errmsg = "write timeout";

                retval = 1;
            }
            catch (Exception ex)
            {
                retval = 2;
                errmsg = ex.Message;
                errcode = -1;

            }

            return retval;


        }


        //--------------------------
        protected override int PollMAV(ref bool mav, ref byte statusbyte, ref int errcode, ref string errmsg)
        {
            //poll for status, return MAV bit 
            //  return 0 if ok, 1 if timeout,  other if other error


            mav = commport.BytesToRead > 0;
            return 0;

        }

        //'--------------------
        protected override int ReceiveByteArray(ref byte[] arr, ref bool EOI, ref int errcode, ref string errmsg)
        {

            //  Public Shared Sub Receive(ByVal board As Integer, ByVal address As Short, ByVal buffer As Byte(), 
            //ByVal count As Integer, ByVal termination As Integer)

            // commport.DiscardInBuffer();   'bad if reread on timeout or not eoi! will destroy data



            var retval = 0;

            string respstr = null;

            try
            {
                respstr = commport.ReadLine();
                arr = System.Text.Encoding.UTF8.GetBytes(respstr);


                EOI = true;
                //because of readline


            }
            catch (TimeoutException )
            {
                errcode = 1;
                errmsg = "read timeout";

                retval = 1;
            }
            catch (Exception ex)
            {
                retval = 2;
                errmsg = ex.Message;
                errcode = -1;

            }

            return retval;




        }




        protected override int ClearDevice(ref int errcode, ref string errmsg)
        {

            //        Public Shared Sub DevClear(ByVal board As Integer, ByVal address As Short)


            try
            {
                commport.DiscardOutBuffer();
                commport.DiscardInBuffer();
                return 0;
            }
            catch
            {
                errcode = -1;
                errmsg = "cannot flush buffer";
                return -1;
            }


        }




        //*********** other private functions



        private object OpenComm(string portspec, int buffersize)
        {

            // return 0 if ok, -1 if error


            commerr = "";

            if (commport == null)
            {
                commport = new SerialPort();
                commport.ReadBufferSize = buffersize;
            }
            else
            {
                if (commport.IsOpen)
                {
                    try
                    {
                        commport.Close();
                    }
                    catch (Exception ex)
                    {
                        commerr = ex.Message;
                        return -1;
                    }
                }
            }


            //extract name:


            string[] parr = portspec.ToUpper().Split(":".ToCharArray());
            //"com1:9600,N,8,2"
            portname = parr[0];

            //settings
            commport.PortName = portname;


            string[] sarr = parr[1].Split(",".ToCharArray());

            commport.BaudRate = int.Parse(sarr[0]);

            switch (sarr[1])
            {
                case "E":
                    commport.Parity = Parity.Even;
                    break;
                case "M":
                    commport.Parity = Parity.Mark;
                    break;
                case "N":
                    commport.Parity = Parity.None;
                    break;
                case "O":
                    commport.Parity = Parity.Odd;
                    break;
                case "S":
                    commport.Parity = Parity.Space;
                    break;
                default:

                    throw new Exception(" invalid comm parity specification");
            }

            commport.DataBits = int.Parse(sarr[2]);

            switch (int.Parse(sarr[3]))
            {
                case 0:
                    commport.StopBits = StopBits.None;
                    break;
                case 1:
                    commport.StopBits = StopBits.One;
                    break;
                case 2:
                    commport.StopBits = StopBits.Two;
                    break;
                default:
                    throw new Exception(" invalid comm stop bits specification");
            }



            if (sarr.Length > 4 && !string.IsNullOrEmpty(sarr[4].Trim()))
            {
                switch (sarr[4].Trim())
                {
                    case "CR":
                        commport.NewLine = Cr;
                        break;
                    case "LF":
                        commport.NewLine = Lf;
                        break;
                    case "CRLF":
                        commport.NewLine = CrLf;
                        break;
                    default:
                        commport.NewLine = sarr[4].Trim();
                        break;
                }

            }

            //exemple "COM1:9600,N,8,2"  -> "com1",9600,Parity.None,8, stopbits.Two

            commport.WriteTimeout = defaultreadtimeout;
            commport.ReadTimeout = defaultwritetimeout;
 

            commport.Open();
            // dont catch errors in constructor


            commport.DiscardOutBuffer();
            commport.DiscardInBuffer();


            return 0;
        }


    }

}