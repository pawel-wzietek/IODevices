
//Class IODevice:  multithreaded GPIB/visa/Com control 
    //(C) P.Wzietek 2016


    //exported classes:
//IODevice   'abstract class from which real devices are derived
//IOQuery   'class passed to callback functions

//all definitions contained in "IODevices" namespace (in VB do not define a "default" namespace for this project)



    // exported IODevice class methods:

    //static methods:
    //   public static void ShowDevices()  //show DevicesForm 
    //   
    //    public static IODevice DeviceByName(string name)   //find device in list using name
    //
    //    public static void DisposeAll()    //dispose all registered devices


    //instance methods:


    //  **** asynchronous queries

    // 'signature of callback functions:
    //  Public Delegate Sub IOCallback(ByVal q As IOQuery)  


    //query: send command and wait for response

    //standard:
    //        public int QueryAsync(string cmd, IOCallback callback, bool retry)

    //complete:

    //        public int QueryAsync(string cmd, IOCallback callback, bool retry, bool cbwait, int tag)

    //2nd version : update textbox with data string
    //        public int QueryAsync(string cmd, TextBox text, bool retry)

    //      public int QueryAsync(string cmd, TextBox text, bool retry, int tag)


    //send command, no response:


    //default behavior :
    //        public int SendAsync(string cmd, bool retry)

    //complete (with callback, probably rarely needed)
    //        public int SendAsync(string cmd, IOCallback callback, bool retry, bool cbwait, int tag)


    //  *******   blocking versions  

    //send command and wait for response

    //        public int QueryBlocking(string cmd, out IOQuery q, bool retry)
    //        public int QueryBlocking(string cmd, out byte[] resparr, bool retry)
    //        public int QueryBlocking(string cmd, out string resp, bool retry)


    //send command, no response

    //        public int SendBlocking(string cmd, bool retry)

    //  ***** other instance methods

    //      public bool IsBlocking()    //        true when blocking call in progress

    //      public int PendingTasks()      //      return number of queries in the queue

    //     public int PendingTasks(string  cmd) // same for a specific command: number of copies of specific command in the queue

    //      public void WaitAsync()
    //       waits until queries queued before the call are done (not until the queue is empty - this may never happen if queries called by timers)

    //     public void AbortAllTasks()

    //   
     //   public void AddToList() 'register device in "devicelist" shown in DeviceForm,  should be called by child class constructors (is not called in the base class constructor to avoid registering ill-defined objects when error)
    //      public void Dispose()


    //version 2: adds possibility to use asynchronous notifying:
    
    // protected void WakeUp()   interrupts waiting for next reading or poll trial


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;

namespace IODevices
{


    using IODeviceForms;

    public class IOQuery
    {
        public string cmd;
        //query identifier 
        public int tag;
        public string ResponseAsString
        {
            //response as string or byte arr depending on query type
            get { return task.parent.ByteArrayToString(resparr); }
        }

        public byte[] ResponseAsByteArray
        {
            get { return resparr; }
        }

        public int status;
        //0:ok, otherwise combination:
        //bit 1:timeout, bit 2 on send(0)/recv(1), bit 3 : other error (see errcode), , bit 4: aborted, bit 5: poll error, bit 8 callback error
        //so if not aborted: status=1 tmo on send; =3 tmo on rcv, =4 other err on send, =6 other err on rcv, 
        // if aborted add 8 , if  poll timeout add 16, 

        //interface error code (if status>0)
        public int errcode;
        //error message
        public string errmsg;
        //when function called
        public DateTime timecall;
        //when device unlocked and operation started
        public DateTime timestart;
        public DateTime timeend;
        //query type :  1: without response (cmd only) '2: with response
        public int type;
        public IODevice device
        {
            get
            {
                if (task != null)
                {
                    return task.parent;
                }
                else
                {
                    return null;
                }
            }
        }

        //abort this task(async or blocking)
        public void AbortRetry()
        {
            task.abort = true;
        }
        //abort all queued async commands and active blocking command
        public void AbortAll()
        {
            task.parent.AbortAllTasks();

        }

        //private fields
        protected internal byte[] resparr;
        //used to access  task fields (abort etc)
        internal IODevice.IOTask task;

        //constructor
        internal IOQuery(int qtype, string command, int qtag)
        {

            cmd = command;
            resparr = null;
            type = qtype;
            timestart = DateTime.MinValue;
            timeend = DateTime.MinValue;
            timecall = DateTime.MinValue;
            task = null;
            errmsg = "";

        }
    }
    //end of IOQuery


    //************             class IODevice                  *******************************
    //*****************************************************************************

    public abstract class IODevice : IDisposable
    {

        public delegate void IOCallback(IOQuery q);
        //signature of callback functions



        //************class IODevice public  variables



        //optional message (status etc.) to display in devices form (eg used by constructors during init)
        public static string statusmsg;


        //-------------------device instance variables/properties

        //used to limit the number of threads 

        public int maxtasks;
        //device name
        public string devname;

        public string devaddr;

        //delay to wait between operations (ms)
        public int delayop;
        //default delay between cmd and read :
        public int delayread;
        // delay before poll/read, especially useful to avoid blocking gpib bus by slow devices when polling is not available

        //default delay before retrying read after timeout
        public int delayrereadontimeout;
        //                                        or delay between polls if polling used 

        //delayread may be overwritten on per task basis
        //cumulative timeout for read (to replace effect of delayread)
        public int readtimeout;
        //delay before retry on timeout
        public int delayretry;

        //if true repeat read if EOI not detected (eg. buffer too small)
        public bool checkEOI;
        //if true serial poll before reading to not to block bus
        public bool enablepoll;
        public byte MAVmask = 16;  //for GPIB, USBTMC-USB488, VXI-11: standard (488.2) mask for MAV status (bit 5 of the status byte), change it for devices not compliant with 488.2 
        
        //remove crlf in ByteArrayToString function 
        public bool stripcrlf;
        //for blocking commands during delays and retry loop
        public bool eventsallowed;

        // error window enabled
        public bool showmessages;
        // if showdevices on startup
        public const bool showdevicesonstartup = true;

        
        public volatile IOQuery lastasyncquery;
        //True  'set to false when debugging a new interface
        public bool catchinterfaceexceptions;
        public bool catchcallbackexceptions;
        //if callback on each retry
        public bool callbackonretry;

        //to override when available
        public virtual bool EnableNotify
        {
            get { return false; }

            set { if (value) { throw new NotImplementedException("Notify not implemented for interface '" + interfacename + "'"); } }
        }


        //*****************    private IODevice variables        *********************************************************
        //   

        //shared variables and functions
        //store refs of all created devices 
        protected static List<IODevice> devlist;
        //shared objects to lock a common bus during read/write, index=interfacelockid 
        private static object[] lockbus;
        //will be set to main form
        private static Form frm;

        internal  static DevicesForm devform = new DevicesForm();
        //to signal when DevicesForm has to update
        internal static bool updated = false; 



        //private instance variables:

        //task queue private variables:
        //to trigger wakeup of async thread
        protected EventWaitHandle queue_event = new AutoResetEvent(false);
        private Thread asyncthread;
        //lock queue during operations on tasks
        private readonly object lockqueue = new object();

        protected Queue<IOTask> tasks;
        //other private
        //set to to true when disposing to terminate properly (so that timer calls cannot fill queue)
        private bool disposing=false;
        //set to true when DisposeDevice() called, so it can be called in main class finalizer
        private bool devicedisposed = false; 

        //index in lockbus: should be set by derived classes to distinguish between interfaces that can be used concurently
        protected int interfacelockid;

        protected string interfacename = "";
        //last error message for errors not related to query (queue full, blocking call in progress) to display in devices form
        protected internal string liberrmsg;
        //to calculate delay
        private DateTime lastoptime;

        //to lock device during cmd-resp sequence
        private readonly object lockdev = new object();

        //per device error message form
        private IODeviceForms.IOmsgForm msgfrm;


        //currently existing async task or nothing if finished (volatile)
        private volatile IOTask currentasynctask;
        //currently existing blocking task or nothing if finished 
        private IOTask currentblockingtask;
        // task that currently has lock on device (may switch between async/blocking during retry loops)
        private volatile IOTask currentactivetask;

        protected IOQuery currentactivequery //to use in implementation eg if need to know the command to format the response accordingly
        {
            get
            {
                IOTask ca = currentactivetask; //make a copy of volatile objects
                if (ca == null) { return null; } else { return ca.query; }
            }
        } 

        //event used to wait (sleep) in async routines so that notify can break waiting
        private EventWaitHandle notify_event = new AutoResetEvent(false);
        //equivalent boolean flag used in waiting loops in blocking calls
        private bool notify_flag;

        //   **********  public methods

        //static constructor
        static IODevice()
        {
            devlist = new List<IODevice>();

            int i = 0;
            lockbus = new object[101];
            for (i = 0; i <= lockbus.Length - 1; i++)
            {
                lockbus[i] = new object();
            }
            frm = Application.OpenForms[0];
            //set to Main Form 
            // (null ref ok if callbacks can be executed on a not-GUI thread, without invoke, and if messages are disabled)

            if (frm == null || (frm.IsDisposed | frm.Disposing))
            {
                throw new Exception("error creating IODevice class: main form null or disposed");
            }

            //other init: send ifc etc:  in derived classes

            //
            updated = false;
            
            if (showdevicesonstartup) { ShowDevices(); }

        }


        //------------------- IODevice public shared methods


        public static void ShowDevices()
        {
            if (devform == null || devform.IsDisposed || devform.Disposing)
            {
                devform = new DevicesForm();
            }
            //if disposing
            try
            {
                devform.Show();
                devform.WindowState = FormWindowState.Normal;
                devform.BringToFront();
            }
            catch
            {
            }

        }


        public static void DisposeAll()
        {

            devform.Close();
            //faster if signal abort for all before shutting down one by one
            foreach (IODevice device in devlist)
            {
                device.AbortAllTasks();
            }


            while (devlist.Count > 0)
            {
                devlist[0].Dispose();
                Application.DoEvents();

            }



        }


        //used by DevicesForm
        public static string[] GetDeviceList(bool details)
        {
            string[] sl = null;
            var n = devlist.Count;
            if (n == 0)
            {
                return null;
            }
            short i = 0;
            string s = null;
            string sc = "";
            string st = "";
            string si = "";
            string scmd = null;


            IOTask cbt = null;
            IOTask cat = null;
            IOTask ct = null;

            for (i = 0; i <= n - 1; i++)
            {
                if (devlist[i] == null)
                    continue;
                si = "(" + devlist[i].interfacename + ")";
                s = devlist[i].devname + "@";

                st = "";

                cbt = devlist[i].currentblockingtask;

                //make a copy of volatile objects!!!
                Interlocked.Exchange(ref cat, devlist[i].currentasynctask);
                Interlocked.Exchange(ref ct, devlist[i].currentactivetask);



                sc = Convert.ToString(devlist[i].devaddr);

                if (ct != null)
                {
                    if (ct.blocking)
                    {
                        st += ", blocking: ";

                    }
                    else
                    {
                        st += ", async: ";
                    }
                    st += "'" + ct.query.cmd + "'  ";

                    if (devlist[i].PendingTasks() > 0)
                    {
                        st += ",  pending:" + devlist[i].PendingTasks().ToString();
                    }
                    if (!string.IsNullOrEmpty(ct.query.errmsg))
                    {
                        st += ",  error:" + ct.query.errmsg;

                        if ((cbt != null && cbt.retry) | (cat != null && cat.retry))
                            st += " (retrying...)";

                    }
                }
                if (!string.IsNullOrEmpty(devlist[i].liberrmsg))
                {
                    st += ", " + devlist[i].liberrmsg;
                }

                if (sl == null)
                {
                    sl = new string[1];
                }
                else
                {
                    Array.Resize(ref sl, sl.Length + 1);
                }

                sl[sl.Length - 1] = si + "  " + s + sc + st;

                //queue
                if (details)
                {


                    //make sure the task list does not change during loop
                    lock (devlist[i].lockqueue)
                    {

                        foreach (IOTask task in devlist[i].tasks)
                        {
                            if ((task != null))
                            {
                                scmd = "    " + "'" + task.query.cmd + "'";
                                Array.Resize(ref sl, sl.Length + 1);
                                sl[sl.Length - 1] = scmd;
                            }
                        }
                    }

                }

            }
            return sl;
        }

        //find device in list using name
        public static IODevice DeviceByName(string name)
        {

            foreach (IODevice d in devlist)
            {
                if (d.devname == name)
                {
                    return d;
                }
            }

            return null;
        }
        // helper function to interpret gpib or visa type address  (simple version, might be rewritten using regex class)
        // address may be just a number eg "9", then board will be set to 0
        //or "0:9",  "GPIB0::9", "GPIB0:9", "GPIB0::9::INSTR" etc 
        //will 

        public static void ParseGpibAddr(string address, out int board, out byte gpibaddr)
        {

            //interpret address




            string[] sarr = null;


            sarr = address.ToUpper().Split(":".ToCharArray());


            try
            {
                switch (sarr.Length)
                {
                    case 0:

                        throw new Exception("invalid address format: " + address);
                    case 1:
                        board = 0;
                        gpibaddr = byte.Parse(sarr[0]);

                        break;
                    default:
                        if (sarr[0].Contains("GPIB"))
                        {
                            string bs = sarr[0].Substring(4);

                            if (bs.Length == 0) board = 0;
                            else board = int.Parse(sarr[0].Substring(4));

                        }
                        else
                        {
                            board = int.Parse(sarr[0]);

                        }

                        int idx = 1;

                        while (idx < sarr.Length && string.IsNullOrEmpty(sarr[idx]))
                        {
                            idx += 1;
                        }
                        if (idx == sarr.Length)
                            throw new Exception("invalid address format: " + address);
                        gpibaddr = byte.Parse(sarr[idx]);

                        break;
                }

            }
            catch
            {
                throw new Exception("invalid address format: " + address);
            }
            if (gpibaddr == 0)
                throw new Exception("invalid address format: " + address);

        }



        //************class IODevice public instance methods *******************************
        //*******************************************


        // constructor

        public IODevice(string name, string addr)
        {
            if (frm == null || (frm.IsDisposed | frm.Disposing))
            {
                throw new Exception("error creating IODevice " + name + ": main form null or disposed");
            }

            devname = name;
            devaddr = addr;
            //name and addr used only to display by devicesform and in error messages

            //set  default options
            delayop = 1;
            delayread = 20;
            delayrereadontimeout = 80;
            delayretry = 1000;
            readtimeout = 5000;//cumulative timeout, independent of interface settings
            showmessages = true;

            catchinterfaceexceptions = true;
            //may be useful to set to false during debugging interface routines
            catchcallbackexceptions = true;
            callbackonretry = true;

            maxtasks = 50;
            checkEOI = true;
            eventsallowed = true;
            enablepoll = true;
            stripcrlf = true;

            //create task queue: 
            tasks = new Queue<IOTask>();

            //start async thread:
            currentasynctask = null;
            currentblockingtask = null;
            currentactivetask = null;
            asyncthread = new Thread(AsyncThreadProc);
            asyncthread.IsBackground = true;
            asyncthread.Start();

            lastoptime = DateTime.Now;

            disposing = false;
            updated = false;
            //add device to global list:
            //  moved to child class constructors (will not be called if exception occurs)



        }

        // interrupts waiting for next reading/poll trial
        // may be called e.g. by "Notify" callback (if defined by the implementation) when data ready
        //can be called from any thread
        protected void WakeUp()

        {

            //we don't know from which thread it will be called therefore signal is sent to both blocking and async tasks (will be rearmed ion next command anyway)
            notify_flag = true;  //used in waitdelay 

            try  //may cause error on disposing
            { notify_event.Set(); }//for async thread

            catch { }
            
        }


        public void Dispose()
        {
            if (disposing)
                return;

            AbortAllTasks();

            disposing = true;
            //prevent new tasks to be appended, signal async thread to exit

            if (showmessages)
            {
                try
                {
                    msg_end_TS();
                }
                catch
                {
                }
            }



            queue_event.Set();
            //set event to wake up async thread 
            EnqueueTask(null); //signal async thread to exit


            //wait 3s for the async  thread to finish gracefully
            if (!asyncthread.Join(3000))
            {
                asyncthread.Abort();
            }


            queue_event.Close();
            notify_event.Close();

            devlist.Remove(this);
            if (msgfrm != null && !msgfrm.IsDisposed)
                msgfrm.Close();

           
                // Release external unmanaged resources
            if (!devicedisposed)
                {
                    try{
                        DisposeDevice();
                        devicedisposed = true; //will  be tested in finalizer
                        GC.SuppressFinalize(this);
                    }
                    catch { }
                }

         }


        
        //finalizer: if not disposed properly make sure the unmanaged 
        //resources are released (eg notify handler uninstalled) before the object is garbage-collected!
        
        ~IODevice()
        {
            try
            {
                if (!devicedisposed)
                {
                    DisposeDevice();
                }
            }
            finally
            {
                devicedisposed = true;  
            }


        } 



        public int PendingTasks()
        {

            int retval = 0;
            lock (lockqueue)
            {
                retval = tasks.Count;
            }

            return retval;

        }

        public int PendingTasks(DateTime t)
        {
            //version counting only tasks with queries called before or at t

            short p = 0;

            lock (lockqueue)
            {

                foreach (IOTask task in tasks)
                {
                    if ((task != null))
                    {
                        if (task.query.timecall <= t)
                        {
                            p += 1;
                        }
                    }
                }
            }

            return p;



        }
        public int PendingTasks(string cmd)
        {
            //version counting only tasks with certain commands
            //case insensitive

            short p = 0;

            //make sure async thread does not remove a task during the loop
            lock (lockqueue)
            {

                foreach (IOTask task in tasks)
                {
                    if ((task != null))
                    {
                        if (task.query.cmd.ToUpper() == cmd.ToUpper())
                        {
                            p += 1;
                        }
                    }
                }
            }

            return p;

        }


        public int PendingTasks(int tag)
        {
            //version counting only tasks with certain tag value


            short p = 0;

            //make sure async thread does not remove a task during the loop
            lock (lockqueue)
            {

                foreach (IOTask task in tasks)
                {
                    if ((task != null))
                    {
                        if (task.query.tag == tag)
                        {
                            p += 1;
                        }
                    }
                }
            }

            return p;



        }

        public void AbortAllTasks()
        {

            lock (lockqueue)
            {

                foreach (IOTask task in tasks)
                {
                    if ((task != null))
                    {
                        task.abort = true;
                    }

                }
                try
                {
                    if (currentasynctask != null)
                        currentasynctask.abort = true;
                    if (currentblockingtask != null)
                        currentblockingtask.abort = true;
                    if (currentactivetask != null)
                        currentactivetask.abort = true;

                }
                catch
                {
                }
            }

        }

        public void WaitAsync()
        {
            WaitAsync(DateTime.Now.AddTicks(2));  //make sure for last command 

        }

        //wait until async queries queued before the call are done (usually set ts to "Now")
        public void WaitAsync(DateTime ts)
        {


            IOTask t = null;
            bool bp = false;
            bool bt = false;
            int p = 0;
            int pt = 0;


            do
            {
                t = currentasynctask;
                p = PendingTasks(ts);
                pt = PendingTasks();
                bp = (p == 0);
                bt = (t == null || t.query.timecall > ts);


                Application.DoEvents();
                Thread.Sleep(1);
            } while (!((bp & bt) | disposing));



        }

        public bool IsBlocking()
        {

            return currentblockingtask != null;

        }





        //query 1st version: callback

        public int QueryAsync(string cmd, IOCallback callback, bool retry)
        {

            return makeQueryAsync(cmd, null, callback, true, retry, 0);

        }

        public int QueryAsync(string cmd, IOCallback callback, bool retry, bool cbwait, int tag)
        {

            return makeQueryAsync(cmd, null, callback, cbwait, retry, tag);

        }
        //2nd version : textbox
        public int QueryAsync(string cmd, TextBox text, bool retry)
        {

            return makeQueryAsync(cmd, text, null, false, retry, 0);
            //'

        }
        public int QueryAsync(string cmd, TextBox text, bool retry, int tag)
        {

            return makeQueryAsync(cmd, text, null, false, retry, tag);

        }

        //send command and wait for response
        protected int makeQueryAsync(string cmd, TextBox text, IOCallback callback, bool cbwait, bool retry, int tag)
        {

            //return 0 if thread run, -1 if too many tasks, -2 if other error

            if (disposing)
                return -2;



            if (PendingTasks() >= maxtasks)
            {
                liberrmsg = "async queue full";
                return -1;

            }


            //make query
            IOQuery query = new IOQuery(2, cmd, tag);
            //type 2wait for  response

            IOTask task = new IOTask(this, query);

            query.timecall = DateTime.Now;



            task.txt = text;
            task.callback = callback;
            task.cbwait = cbwait;
            task.retry = retry;
            task.events = false;



            //append 
            EnqueueTask(task);
            return 0;

        }



        //send command, no response
        protected int makeSendAsync(string cmd, IOCallback callback, bool cbwait, bool retry, int tag)
        {


            //return 0 if ok, -1 if too many tasks, -2 if disabled 

            if (disposing)
                return -2;



            if (PendingTasks() >= maxtasks)
            {
                liberrmsg = "async queue full";
                return -1;

            }

            IOQuery query = new IOQuery(1, cmd, tag);
            //type 1 no resp

            IOTask task = new IOTask(this, query);

            query.timecall = DateTime.Now;

            task.callback = callback;
            task.cbwait = cbwait;
            task.retry = retry;
            task.events = false;

            //append 
            EnqueueTask(task);

            return 0;

        }
        //send command, no response,
        public int SendAsync(string cmd, IOCallback callback, bool retry, bool cbwait, int tag)
        {

            return makeSendAsync(cmd, callback, cbwait, retry, tag);

        }
        //default behavior : no callback
        public int SendAsync(string cmd, bool retry)
        {

            return makeSendAsync(cmd, null, false, retry, 0);

        }



        //blocking versions  ********************

        //send command and wait for response


        public int QueryBlocking(string cmd, out IOQuery q, bool retry)
        {


            //return -1 if blocking call in progress (may happen if events allowed), -2 disposing
            //otherwise return status as in IOQuery (0 if ok)


            q = new IOQuery(2, cmd, 0);



            if (disposing)
            {
                q.status = -2;
                return q.status;
            }


            if (currentblockingtask != null)
            {
                q.status = -1;
                q.errmsg = "blocking call in progress";
                return q.status;
                //necessary because wait loop until bus unlocked (otherwise will wait forever since is on the same thread)
            }



            IOTask t = new IOTask(this, q);

            t.query.timecall = DateTime.Now;

            t.cbwait = false;
            //no meaning here
            t.retry = retry;
            t.events = eventsallowed;

            t.delayread = delayread;


            //run directly from calling thread but lock device so that Async functions can be used concurrently
            t.blocking = true;
            Interlocked.Exchange(ref currentblockingtask, t); updated = false;

            t.TaskProc();
            t.blocking = false;

            

            Interlocked.Exchange(ref currentblockingtask, null); updated = false;

            return q.status;

        }




        public int QueryBlocking(string cmd, out string resp, bool retry)
        {


            //return -1 if blocking call in progress (may happen if events allowed), -2 disabled
            //otherwise return status as in IOQuery (0 if ok)

            resp = null;

            if (disposing)
                return -2;

            if (currentblockingtask != null)
            {
                liberrmsg = "blocking call in progress";
                return -1;
            }

            IOQuery q = null;
            //or resp = ""  'to avoid null ref exceptions for lazy programmers?
            int st = QueryBlocking(cmd, out q, retry);

            if (q != null)
            {
                resp = q.ResponseAsString;
            }

            return st;

        }

        //********************************
        public int QueryBlocking(string cmd, out byte[] resparr, bool retry)
        {

            //return -1 if blocking call in progress (may happen if events allowed), -2 disabled
            //otherwise return status as in IOQuery (0 if ok)

            resparr = null;

            if (disposing)
                return -2;

            if (currentblockingtask != null)
            {
                liberrmsg = "blocking call in progress";
                return -1;
            }

            IOQuery q;

            int st = QueryBlocking(cmd, out q, retry);

            if (q != null)
            {
                resparr = q.ResponseAsByteArray;
            }

            return st;


        }


        //send command, no response

        public int SendBlocking(string cmd, bool retry)
        {


            //return -1 if blocking call in progress (may happen if events allowed)
            //return -2 if disposing
            //otherwise return status as in IOQuery 

            if (disposing)
                return -2;


            if (currentblockingtask != null)
            {
                liberrmsg = "blocking call in progress";
                return -1;
            }

            IOQuery query = new IOQuery(1, cmd, 0);



            IOTask t = new IOTask(this, query);

            t.query.timecall = DateTime.Now;


            t.cbwait = false;
            //no meaning here
            t.retry = retry;
            t.events = eventsallowed;

            t.delayread = delayread;
            //in blocking commands may help not to block bus and GUI events when polling is not available

            //run directly from calling thread but lock device so that Async functions can be used concurrently
            t.blocking = true;
            Interlocked.Exchange(ref currentblockingtask, t); updated = false;

            t.TaskProc();
            t.blocking = false;

           


            Interlocked.Exchange(ref currentblockingtask, null); updated = false;

            return query.status;

        }


        public virtual string ByteArrayToString(byte[] arr)
        {

            string s = null;




            try
            {
                var len = arr.Length;
                //may cause exception if no array
                //remove terminating LF, CR if any
                if (stripcrlf)
                {
                    if (len > 0 && arr[len - 1] == 10)
                        len = len - 1;
                    //ignore lf at end
                    if (len > 0 && arr[len - 1] == 13)
                        len = len - 1;
                    //ignore cr
                }

                s = System.Text.Encoding.UTF8.GetString(arr, 0, len);
            }
            catch
            {
                s = null;

            }




            return s;
        }




        //*****************  interface abstract methods that have to be defined
        //
        // all functions  should return 0 if ok, 1 if timeout,  other value if other error
        // the functions should catch all interface exceptions (otherwise the "catchinterfaceexceptions" flag should be set)
        // if there is an error (returned value different from 0 or 1)  the information should be returned in errcode and errmsg
        // errcode and errmsg are just for display and are not interpreted by the class

        protected abstract int ClearDevice(ref int errcode, ref string errmsg);
        protected abstract int Send(string cmd, ref int errcode, ref string errmsg);
        protected abstract int PollMAV(ref bool mav, ref byte statusbyte, ref int errcode, ref string errmsg);
        //poll for status, return status byte and MAV bit 
        //statusbyte is for user info (displayed in message window) and also to allows to easily override this method reinterpreting the status byte for devices not compliant with 488.2 (eg.Lakeshore),
        //the function should interpret it and set the mav flag which will be used by the class to decide when to start reading data
        //(for interfaces that don't implement this feature (serial) the function will set it to true)
        // the function will be called repeatedly until it sets mav to true (or until cumulative timeout period "readtimeout" elapses)
        protected abstract int ReceiveByteArray(ref byte[] arr, ref bool EOI, ref int errcode, ref string errmsg);
        //if the function returns 1 (timeout), the function will be called repeatedly (until cumulative timeout period "readtimeout" elapses)
        //EOI is the "message complete" status flag. If set to false the function will be called repeatedly and data chunks assembled until it is set to true (or timeout period elapses)
        //(usually EOI will be set to false when buffer gets full)
        protected abstract void DisposeDevice();  //release unmanaged resources if any


        //related overridable methods : 
        //      Protected Overridable Function ByteArrayToString(ByVal arr() As Byte) As String




        //must be called only by child class constructors
        public void AddToList()
        {

            
            devlist.Add(this);

            //force updating DevicesForm
            updated = false;

        }

        //*******************************  IODevice private methods


        //queue handling with lock:

        private void EnqueueTask(IOTask task)
        {
            lock (lockqueue)
            {
                if (tasks.Count <= maxtasks | task == null)
                {
                    tasks.Enqueue(task);
                }
            }
            queue_event.Set();//set event to wake up async thread
            updated = false;

        }
        //return true if there was a task (could be nothing)
        private bool DequeueTask()
        {
            lock (lockqueue)
            {
                if (tasks.Count > 0)
                {
                    currentasynctask = tasks.Dequeue();
                    updated = false;
                    return true;
                }
                else
                {

                    return false;
                }

            }

        }


        //asyncthread
        private void AsyncThreadProc()
        {

            while (!disposing)
            {

                if (DequeueTask())
                {
                    if (currentasynctask == null)
                    {
                        return;
                        //end async thread if null task has been queued
                        //do task
                    }
                    else
                    {
                        if (!disposing)
                        {
                            currentasynctask.TaskProc();
                            Thread.Sleep(1);
                            //give a chance to blocking calls 
                        }
                    }

                    //no more tasks
                }
                else
                {
                    currentasynctask = null; 
                    //signal when current task finished
                    if (!disposing)
                        queue_event.WaitOne(200);
                    // No more tasks - wait for a signal (timeout 200 to ensure exit when disposing) 

                }
            }
        }

        private static bool Ismainthread()
        {

            if (frm == null)
            {
                return true;
                //form not defined eg console app ?
            }
            else
            {
                if (!(frm.IsDisposed | frm.Disposing))
                {
                    return !frm.InvokeRequired;
                }
                else
                {
                    //Debug.Print("error in IOTask.Ismainthread: form disposed")
                    return false;
                    //safer
                }
            }


        }




        //must be invoked from gui thread
        //make/show error message form when closing device
        private void msg_end(string msg)
        {


            if (msgfrm == null || msgfrm.IsDisposed)
                msgfrm = new IODeviceForms.IOmsgForm();
            msgfrm.shutdownmsg(msg);
        }

        //in case dispose called by another thread?
        private void msg_end_TS()
        {


            try
            {

                if (frm.InvokeRequired)
                {
                    frm.Invoke(new Action<string>(msg_end), new object[] { devname });

                }
                else
                {
                    msg_end(devname);
                }
            }
            catch
            {
            }

        }


        //************************inner class GpibTask: one instance for each query created (tasks will be queued)

        protected internal class IOTask
        {
            //copy of parent to access its members
            protected internal IODevice parent;


            protected internal IOQuery query;
            //fields used to return result
            protected internal TextBox txt;

            protected internal IOCallback callback;
            //delay between cmd and read : to avoid blocking gpib bus by slow devices
            protected internal int delayread;

            //set true when called on main thread from blocking functions
            protected internal bool blocking;
            //thread wait (not locked) until callback resumes
            protected internal bool cbwait;
            //retry on timeout
            protected internal bool retry;
            //abort signal
            protected internal bool abort;
            //if doevents allowed on retry loop: set false in async and "eventsallowed" in blocking
            protected internal bool events;
            private string CrLf = "\r\n"; //System.Text.Encoding.UTF8.GetString(new byte[2] { 13, 10 });


            //constructor

            public IOTask(IODevice parentdev, IOQuery q)
            {

                parent = parentdev;
                query = q;
                query.task = this;



                delayread = parent.delayread;
                txt = null;

                callback = null;
                abort = false;
                blocking = false;

            }
            //private methods

            //wait when in GUI thread
            private void waitevents(int delms, bool allowbreak)
            {

                const short sleepms = 3;

                DateTime t1 = default(DateTime);
                double msecs = 0;

                if (delms < 0)
                    return;

                t1 = DateTime.Now;

               
                do
                {
                    msecs = DateTime.Now.Subtract(t1).TotalMilliseconds;
                    //total!!! 

                    if (events)
                        System.Windows.Forms.Application.DoEvents();
                    Thread.Sleep(sleepms);
                    //yield shortly 
                } while (!(msecs >= delms || (allowbreak & parent.notify_flag)));

                parent.notify_flag = false; //rearm

            }

            //wait delay (in ms) counted from "fromtime" (usually set from=Now)

            private void waitdelay(DateTime fromtime, int delay, bool allowbreak)
            {


                int m = 0;

                m = delay - DateTime.Now.Subtract(fromtime).Milliseconds;

                if (m > 0)
                {
                    if (!Ismainthread())
                    {
                        if (allowbreak)
                        { parent.notify_event.WaitOne(m); } // to enable wake up by Wakeup()
                        else
                        {Thread.Sleep(m);}  
                    }
                    else
                    {
                        waitevents(m, allowbreak);
                    }
                }





            }

            private void rearmforwakeup()  //used when sending new command
            {
                if (Ismainthread())
                { parent.notify_flag = false; }
                else
                { parent.notify_event.Reset(); } 
            }


            //poll for status, wait (readtimeout) until MAV bit set 
            private void waitMAV()
            {

                //polling frequency defined by delayrereadontimeout

                bool rereadflag = false;

                int pollcount = 0;
                //for tests
                DateTime startpoll = default(DateTime);
                bool mav = false;
                byte statusbyte = 0;

                int result = 0;
                string errmsg = "";


                startpoll = DateTime.Now;
                //-----------reread:  loop here to repeat poll 
                do
                {
                    rereadflag = false;

                    bool buslocked = false;


                    //set <0 if no bus lock needed (eg. serial)
                    if (parent.interfacelockid >= 0)
                    {
                        buslocked = false;
                        //if already locked by another device
                        while (!buslocked & !abort & !parent.disposing)
                        {
                            //******************************** critical section for the bus: locked at global class level
                            // use Monitor.TryEnter  instead of synclock to avoid deadlocks or freezing GUI (when a blocking query waits for lock, if it was freezing GUI  then "Invoke" would be blocked too) 
                            buslocked = Monitor.TryEnter(IODevice.lockbus[parent.interfacelockid], 5);
                            if (!buslocked)
                            {
                                Thread.Sleep(1);
                                //true only when the call belongs to a task performing a "blocking" routine from gui thread
                                if (blocking)
                                {
                                    System.Windows.Forms.Application.DoEvents();
                                    //must give a chance to process events here, to avoid freezing GUI, when blocking call waits until unlocked by asyncthread
                                }
                            }
                        }
                    }


                    //may abort/dispose on waiting for lock 
                    if (buslocked | parent.interfacelockid < 0)
                    {

                        try
                        {
                            result = parent.PollMAV(ref mav, ref statusbyte, ref query.errcode, ref errmsg);
                        }
                        catch (Exception ex)
                        {
                            query.errmsg = "exception in 'PollMav': " + CrLf + ex.Message;
                            if (ex.InnerException != null)
                                query.errmsg += CrLf + ex.InnerException.Message;
                            result = -1;
                            query.errcode = -1;
                            query.status = 6 + 16;
                            if (!parent.catchinterfaceexceptions)
                                throw;
                            //re-throw the exception with same stack
                        }
                        finally
                        {
                            if (buslocked)
                                Monitor.Exit(IODevice.lockbus[parent.interfacelockid]);
                        }


                        pollcount += 1;

                        //status=1 tmo on send,  =3 tmo on rcv, =4 other err on send, =6 other err on rcv
                        if (mav && result == 0)
                        {
                            query.status = 0;

                        }
                        else
                        {
                            
                        double d = DateTime.Now.Subtract(startpoll).TotalMilliseconds;

                        if (d < parent.readtimeout)
                            {
                                rereadflag = true;
                                //retry on any error (dont set timeout status yet)
                                pollcount += 1;
                            }
                            else
                            {
                                rereadflag = false;
                                //status=1 tmo on send; =3 tmo on rcv, =4 other err on send, =6 other err on rcv
                                query.status = 3 + 16;
                                //definitive read timout : poll problem

                                switch (result)
                                {
                                    case 0:
                                        query.errmsg = "poll timeout: MAV not set, status byte=" + statusbyte;
                                        break;
                                    case 1:
                                        query.errmsg = "poll timeout: cannot get status byte";
                                        break;
                                    default:
                                        query.errmsg = "poll error : " + errmsg;
                                        query.status = 6 + 16; //  other error on poll
                                        break;
                                }

                            }
                        }


                        if (rereadflag)
                            waitdelay(DateTime.Now, parent.delayrereadontimeout, true);
                    }

                } while (!(!rereadflag | abort | parent.disposing));
                //(though no way to abort from msg window)


               

            }



            private void sendcmd()
            {

                int result = 0;

                bool buslocked = false;


                //set <0 if no bus lock needed (eg. serial)
                if (parent.interfacelockid >= 0)
                {
                    buslocked = false;
                    //if already locked by another device
                    while (!buslocked & !abort & !parent.disposing)
                    {
                        //******************************** critical section for the bus: locked at global class level
                        // use Monitor.TryEnter  instead of synclock to avoid deadlocks or freezing GUI (when a blocking query waits for lock, if it was freezing GUI  then "Invoke" would be blocked too) 
                        buslocked = Monitor.TryEnter(IODevice.lockbus[parent.interfacelockid], 5);
                        if (!buslocked)
                        {
                            Thread.Sleep(1);
                            //true only when the call belongs to a task performing a "blocking" routine from gui thread
                            if (blocking)
                            {
                                System.Windows.Forms.Application.DoEvents();
                                //must give a chance to process events here, to avoid freezing GUI, when blocking call waits until unlocked by asyncthread
                            }
                        }
                    }
                }


                //may abort/dispose on waiting for lock  
                if (buslocked | parent.interfacelockid < 0)
                {

                    try
                    {
                        result = parent.Send(query.cmd, ref query.errcode, ref query.errmsg);

                    }
                    catch (Exception ex)
                    {
                        query.errmsg = "exception in 'Send': " + CrLf + ex.Message;
                        if (ex.InnerException != null)
                            query.errmsg += CrLf + ex.InnerException.Message;
                        result = -1;
                        query.errcode = -1;
                        query.status = 4;
                        if (!parent.catchinterfaceexceptions)
                            throw;
                        //re-throw the exception with same stack
                    }
                    finally
                    {
                        if (buslocked)
                            Monitor.Exit(IODevice.lockbus[parent.interfacelockid]);
                    }


                    //so status=1 tmo on send;
                    // =3 tmo on rcv, =4 other err on send, =6 other err on rcv
                    if (result == 0)
                    {
                        query.status = 0;

                    }
                    else
                    {
                        //timeout 
                        if (result == 1)
                        {
                            query.status = 1;
                            //send tmo
                            query.errmsg = "write timeout";
                            // Message "aborted" not clear...

                        }
                        else
                        {
                            query.status = 4;
                        }
                    }
                }

            }


            //'--------------------

            private void getresponse()
            {
                bool rereadflag = false;
                int readcount = 0;

                //for tests
                DateTime startread = default(DateTime);

                int result = 0;


                startread = DateTime.Now;
                //-----------reread:  loop here to repeat read on GPIB timeout without setting timeout status (slow devices)
                do
                {
                    rereadflag = false;
                    bool EOI = true;
                    byte[] arr = null;

                    bool buslocked = false;


                    //set <0 if no bus lock needed (eg. serial)
                    if (parent.interfacelockid >= 0)
                    {
                        buslocked = false;
                        //if already locked by another device
                        while (!buslocked & !abort & !parent.disposing)
                        {
                            //******************************** critical section for the bus: locked at global class level
                            // use Monitor.TryEnter  instead of synclock to avoid deadlocks or freezing GUI (when a blocking query waits for lock, if it was freezing GUI  then "Invoke" would be blocked too) 
                            buslocked = Monitor.TryEnter(IODevice.lockbus[parent.interfacelockid], 5);
                            if (!buslocked)
                            {
                                Thread.Sleep(1);
                                //true only when the call belongs to a task performing a "blocking" routine from gui thread
                                if (blocking)
                                {
                                    System.Windows.Forms.Application.DoEvents();
                                    //must give a chance to process events here, to avoid freezing GUI, when blocking call waits until unlocked by asyncthread
                                }
                            }
                        }
                    }


                    //may abort/dispose on waiting for lock 
                    if (buslocked | parent.interfacelockid < 0)
                    {


                        try
                        {
                            result = parent.ReceiveByteArray(ref arr, ref EOI, ref query.errcode, ref query.errmsg);

                        }
                        catch (Exception ex)
                        {
                            query.errmsg = "exception in 'ReceiveByteArray': " + CrLf + ex.Message;
                            if (ex.InnerException != null)
                                query.errmsg += CrLf + ex.InnerException.Message;
                            result = -1;
                            query.errcode = -1;
                            query.status = 6;
                            if (!parent.catchinterfaceexceptions)
                                throw;
                            //re-throw the exception with same stack
                        }
                        finally
                        {
                            if (buslocked)
                                Monitor.Exit(IODevice.lockbus[parent.interfacelockid]);
                        }


                        if (result == 0 & arr != null)
                        {
                            copyorappend_array(arr, ref query.resparr);
                            
                            startread = DateTime.Now;
                            //redefine timeout condition after successful reading

                        }

                        bool part = !EOI & parent.checkEOI;

                        //reread: append (works if EOI false (buffer too small) but usually causes errors if aborted during IO)
                        if (result == 0 & part)
                            rereadflag = true;
                        //no delay if reread because no EOI
                        readcount += 1;


                        //so status=1 tmo on send;
                        // =3 tmo on rcv, =4 other err on send, =6 other err on rcv

                        if (result == 0 & !part)
                        {
                            query.status = 0;

                        }
                        else
                        {
                            //check timeout condition
                            if (result == 1 || (result==0 && part))
                            {

                                //first check if reread:
                                //if cumulated time < readtmo then just repeat 
                                double d = DateTime.Now.Subtract(startread).TotalMilliseconds;

                                if (d < parent.readtimeout)
                                {
                                    rereadflag = true;
                                    //dont set timeout status yet
                                    readcount += 1;

                                }
                                else
                                {
                                    //status=1 tmo on send; =3 tmo on rcv, =4 other err on send, =6 other err on rcv
                                    rereadflag = false;
                                    query.status = 3;
                                    //definitive read timout
                                    query.errmsg = "read timeout";
                                    //ex.Message
                                    if (part)
                                        query.errmsg += "  (EOI not set)";
                                }
                                //other error
                            }
                            else
                            {
                                query.status = 6;


                            }
                        }

                        if (rereadflag)
                            waitdelay(DateTime.Now, parent.delayrereadontimeout,true);
                    }

                } while (!(!rereadflag | abort | parent.disposing));
                //(though no way to abort from msg window)


            }


            private void cleardevice()
            {
                int result = 0;
                string errmsg = "";
                string lasterrmsg = query.errmsg;
                int count = 0;

                //--------- retry clearing device
                do
                {
                    count += 1;
                    bool buslocked = false;


                    //set <0 if no bus lock needed (eg. serial)
                    if (parent.interfacelockid >= 0)
                    {
                        buslocked = false;
                        //if already locked by another device
                        while (!buslocked & !abort & !parent.disposing)
                        {
                            //******************************** critical section for the bus: locked at global class level
                            // use Monitor.TryEnter  instead of synclock to avoid deadlocks or freezing GUI (when a blocking query waits for lock, if it was freezing GUI  then "Invoke" would be blocked too) 
                            buslocked = Monitor.TryEnter(IODevice.lockbus[parent.interfacelockid], 5);
                            if (!buslocked)
                            {
                                Thread.Sleep(1);
                                //true only when the call belongs to a task performing a "blocking" routine from gui thread
                                if (blocking)
                                {
                                    System.Windows.Forms.Application.DoEvents();
                                    //must give a chance to process events here, to avoid freezing GUI, when blocking call waits until unlocked by asyncthread
                                }
                            }
                        }
                    }


                    //may abort/dispose on waiting for lock 
                    if (buslocked | parent.interfacelockid < 0)
                    {
                        try
                        {
                            result = parent.ClearDevice(ref query.errcode, ref errmsg);
                        }
                        catch (Exception ex)
                        {
                            query.errmsg = "exception in 'ClearDevice': " + CrLf + ex.Message;
                            if (ex.InnerException != null)
                                query.errmsg += CrLf + ex.InnerException.Message;
                            result = -1;
                            query.errcode = -1;
                            if (!parent.catchinterfaceexceptions)
                                throw;
                            //re-throw the exception with same stack
                        }
                        finally
                        {
                            if (buslocked)
                                Monitor.Exit(IODevice.lockbus[parent.interfacelockid]);
                        }


                        //query.status = 0  'don't reinitialize status if cleared (needed to retry!)
                        if (result != 0)
                        {
                            query.status = 4;
                            if (count == 1) { query.errmsg += CrLf + "cannot clear device:" + errmsg; }
                            runsub(msg_on, query, false);
                            waitdelay(DateTime.Now, parent.delayretry, true);

                        }
                        else { query.errmsg = lasterrmsg + CrLf + "device cleared"; }
                       
                    }
                    //retry clearing device
                } while (!(result == 0 | !retry | abort | parent.disposing));

            }

            //main IO task


            protected internal void TaskProc()
            {

                query.resparr = null;
                query.status = 0;
                query.errmsg = "";
                query.timestart = DateTime.MinValue;

                parent.liberrmsg = "";
                //reset when task starts


                bool locked = false;
                //device lock




                if (!parent.disposing & !abort)
                {
                    //-----------------   query retry loop
                    do
                    {
                        // try to lock device:
                        locked = false;
                        //if already locked by async call
                        while (!locked & !abort & !parent.disposing)
                        {
                            //******************************** critical section for the device: locked at device instance level
                            // use Monitor.TryEnter  instead of synclock to avoid deadlocks or freezing GUI (when a blocking query waits for lock, if it was freezing GUI  then "Invoke" would be blocked too) 
                            locked = Monitor.TryEnter(parent.lockdev, 5);
                            if (!locked)
                            {
                                Thread.Sleep(1);
                                //true only when the call belongs to a task performing a "blocking" routine from gui thread
                                if (blocking)
                                {
                                    System.Windows.Forms.Application.DoEvents();
                                    //must give a chance to process events here, to avoid freezing GUI, when blocking call waits until unlocked by asyncthread
                                }
                            }
                        }

                        //may abort/dispose on waiting for lock  
                        if (locked)
                        {

                            try
                            {                               

                                parent.currentactivetask = this; updated = false;
                                //repeat in case another thread changes it during retry

                                if (query.timestart == DateTime.MinValue)
                                    query.timestart = DateTime.Now;
                                //set time start when device available and gpib op starts

                                //make sure there is minimum delay counted from end of last operation (for slow devices)
                                //wait when device locked so cannot be preempted by other commands 
                                waitdelay(parent.lastoptime, parent.delayop,false);

                                query.status = 0;
                                query.errmsg = "";

                              
                                //-----------send command
                                if (!string.IsNullOrEmpty(query.cmd))
                                {
                                    rearmforwakeup();  //rearming before sending a new command rather than before starting to wait to avoid race condition
                                    //if no command sent then only auto-rearming after a delay, so that driver callbacks can anticipate delays 
                                    sendcmd();
                                }

                                //------------- if send succeeded then  reading (otherwise will retry)


                                if (!abort & !parent.disposing & (query.status == 0) & (query.type == 2))
                                {

                                    waitdelay(DateTime.Now, delayread, true);

                                    if (parent.enablepoll)
                                    {
                                        waitMAV();
                                        //serial poll periodically to check when data available
                                    }
                                    
                                    //If polling ok 
                                    if ((query.status == 0) & !abort & !parent.disposing)
                                    {
                                        getresponse();
                                    }

                                }
                                //  end of read

                               
                                if (query.status != 0 & !abort & !parent.disposing)
                                {
                                    waitdelay(DateTime.Now, parent.delayretry, false);
                                    //wait before retry (necessary in blocking cmd)
                                    cleardevice();
                                }
                            }
                            finally
                            {
                                parent.lastoptime = DateTime.Now;
                                //mark timestamp of end of last op
                                Monitor.Exit(parent.lockdev);
                                //release lock on device
                                parent.currentactivetask = null; updated = false;
                            }
                        }



                        //update before retry loop:
                        if (abort)
                            query.status += 8;
                        //will be visible only in lastquery

                        if (query.status > 0 & !abort & !parent.disposing)
                        {
                            runsub(msg_on, query, false);
                            //call before retry
                            if (retry & parent.callbackonretry)
                            {

                                runsub(callback, query, cbwait);
                            }

                        }


                        if (query.status == 0 | abort | parent.disposing)
                        {
                            runsub(msg_off, query, false);
                            //close msg if ok or aborted by user

                            if (query.status == 0)
                                query.errmsg = "";
                            else
                                query.errmsg = "task aborted by user";
                        }

                        //       ------------  main retry loop 
                    } while (!(query.status == 0 | !retry | abort | parent.disposing));

                }

                query.timeend = DateTime.Now;




                if (query.status == 0 & query.type == 2 & txt != null & !abort)
                {
                    settext_TS(txt, parent.ByteArrayToString(query.resparr));

                }

                if (!blocking)
                    parent.lastasyncquery = query;


                if (!(retry & parent.callbackonretry & query.status > 0) & !abort & !parent.disposing)
                {
                    runsub(callback, query, cbwait);
                    //will  set status to -1 on exception in callback
                }




            }

            //append front to back
            private void copyorappend_array(byte[] front, ref byte[] back)
            {
                if (front == null)
                    return;
                //just leave as is


                if (back == null)  //just copy array
                {
                    back = front;

                }
                else
                {
                    var backOriginalLength = back.Length;
                    Array.Resize(ref back, backOriginalLength + front.Length);
                    Array.Copy(front, 0, back, backOriginalLength, front.Length);

                }

            }




            //must be invoked from gui thread
            private void msg_off(IOQuery q)
            {


                if (parent.showmessages)
                {
                    if (parent.msgfrm != null && !parent.msgfrm.IsDisposed)
                    {
                        parent.msgfrm.Close();
                    }
                }

            }
            //param q to init msgform
            //must be invoked from gui thread

            //make/show error message form, 
            private void msg_on(IOQuery q)
            {
                if (query.status == 0)
                    return;


                if (parent.showmessages)
                {
                    if (parent.msgfrm == null || parent.msgfrm.IsDisposed)
                        parent.msgfrm = new IODeviceForms.IOmsgForm();

                    parent.msgfrm.showit(q);
                }


            }


            //thread-safe version of  txt.Text = s 

            private static void settext_TS(TextBox txt, string s)
            {


                if (txt != null && !(txt.IsDisposed | txt.Disposing))
                {
                    if (txt.InvokeRequired)
                    {
                      
                        //use action(of ...) to make delegate
                        //Invoke immediately: will wait until done (so that sub will always be executed after)
                        //invokes itself: will fall on "else" when in GUI
                        //should be immediate in case a textbox is used to retrieve data

                        txt.Invoke(new Action<TextBox, string>(settext_TS), new object[] {
						txt,
						s
					});
                    }
                    else
                    {
                        txt.Text = s;
                    }
                }


            }



            private void runsub(IOCallback dsub, IOQuery q, bool immediate)
            {


                if (dsub == null)
                    return;

                IOCallback mon = msg_on;
                IOCallback moff = msg_off;

                bool cb = (dsub != mon & dsub != moff);
                //callback


                if (IODevice.frm == null)
                {
                    dsub.Invoke(q);
                    //form not defined eg console app?(didnt test with console app) - but then callback has to be thread-safe!
                }
                else
                {
                    if (!(IODevice.frm.IsDisposed | IODevice.frm.Disposing))
                    {
                        if (IODevice.frm.InvokeRequired)
                        {
                            try
                            {
                                if (immediate & !parent.disposing)
                                {
                                    IODevice.frm.Invoke(new Action<IOCallback, IOQuery, bool>(runsub), new object[] {
									dsub,
									q,
									true
								});
                                    //deferred call("post message")
                                }
                                else
                                {
                                    IODevice.frm.BeginInvoke(new Action<IOCallback, IOQuery, bool>(runsub), new object[] {
									dsub,
									q,
									false
								});
                                }
                            }
                            catch (TargetInvocationException)
                            {
                                Debug.Print("TargetInvocationException when invoking callback:  main form disposed?");
                                // throw new Exception("cannot invoke callback: called with main form disposed ?"); //may happen on closing app

                                //unhandled exceptions in callback function get here 
                            }
                            catch (Exception ex)
                            {
                                //if not catched the debugger will stop on "invoke" line, it is more convenient to catch them here and display a message

                                //check in case an error in msg_on
                                if (cb & parent.catchcallbackexceptions)
                                {
                                    if (q.status == 0)
                                    {
                                        q.errmsg = "Exception message: " + ex.Message;
                                        //additional error
                                    }
                                    else
                                    {
                                        q.errmsg = " also: unhandled exception in user callback function: " + ex.Message;
                                    }

                                    q.status += 256;
                                    //    'internally using bit 9 to signal callback exception, will be removed by msgon

                                    runsub(msg_on, q, false);

                                }
                                else
                                {
                                    throw;
                                    //rethrow callback exception
                                }

                            }
                        }
                        else
                        {
                            dsub.Invoke(q);
                        }
                    }
                    else
                    {
                        //frm disposed: should not happen unless a device is created after main form is disposed but then we are in trouble!  (fatal error)
                         Debug.Print("async query error : main form disposed, cannot invoke callback");
                       // throw new Exception("callback called with main form disposed"); //may happen on closing app
                    }
                }



            }
            //*****************************************end of inner IOTask class





        }

    }
}