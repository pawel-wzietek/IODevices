using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

using IODevices;

namespace testIODevice
{


	public partial class Formtest
	{



		public IODevice dev1;

		public IODevice dev2;

        private string CrLf = "\r\n"; //System.Text.Encoding.UTF8.GetString(new byte[2] { 13, 10 });

		public void formtest_close()
		{
			IODevice.DisposeAll();
		}


		private void Formtest_Load(System.Object sender, System.EventArgs e)
		{
	   
        lstIntf1.Items.Add("Visa");
        lstIntf1.Items.Add("GPIB:ADLink");
        lstIntf1.Items.Add("gpib488.dll");
        lstIntf1.Items.Add("Com port");
        lstIntf1.SelectedIndex = 0;


    
        lstIntf2.Items.Add("Visa");
        lstIntf2.Items.Add("GPIB:ADLink");
        lstIntf2.Items.Add("gpib488.dll");
        lstIntf2.Items.Add("Com port");
        lstIntf2.SelectedIndex = 0;
		}
		public IODevice CreateDevice(string name, string address, int interfacetype)
		{

			//this function returns a generic IODevice object: here we can define devices polymorphically 
			// because in this test code we don't use many interface-specific methods

			//this is convenient if there are not too many interface-dependent options  (eg visa attributes, see below)
			//  (but otherwise it is still possible to bind statically without any change in the program other than initialization)

			IODevice dev = null;


			try {
				switch (interfacetype) {
				
					case 0:
						dev = new VisaDevice(name, address);
						break;
                    case 1:
                        dev = new GPIBDevice_ADLink(name, address);
                        break;
                    case 2:
                        dev = new GPIBDevice_gpib488(name, address);
                        break;
					case 3:
						dev = new SerialDevice(name, address);
						break;
					default:
						return null;
				}



			//(constructor exception: most often "dll not found")
			} catch (Exception ex) {
				string msg = " cannot create device " + name +CrLf + ex.Message;
                if (ex.InnerException != null && ex.InnerException.Message != ex.Message)
					msg = msg + CrLf+ ex.InnerException.Message;
				MessageBox.Show(msg);
				dev = null;
                IODevice.statusmsg = "";
			}

			//option to debug interface routines, may remove this line in final version:
			//If dev IsNot Nothing Then dev.catchinterfaceexceptions = False 

			return dev;


		}

        private void setnotify(IODevice dev)
        {

            try
            {
                dev.EnableNotify = true;  //default implementation will throw an exception if not available for the selected interface

                int result = dev.SendBlocking("*SRE 16", false);// set bit 4 in Service Request Enable Register, so that the MAV status will set SRQ
																//(more generally, if not 488.2 compatible: send the MAVmask) 
                if (result == 0)
                {
                    dev.delayread = 1000;
                    dev.delayrereadontimeout = 1000; //set long wait delays (will be interrupted anyway)
                }
            }
            catch (Exception ex)
            {
                string msg = " cannot set EnableNotify for device " + dev.devname + CrLf + ex.Message;
                if (ex.InnerException != null && ex.InnerException.Message != ex.Message)
                    msg = msg + CrLf + ex.InnerException.Message;
                MessageBox.Show(msg);
            }


        }


		private void btncreate_Click(System.Object sender, System.EventArgs e)
		{

			dev1 = CreateDevice(txtname1.Text, txtaddr1.Text, lstIntf1.SelectedIndex);
			dev2 = CreateDevice(txtname2.Text, txtaddr2.Text, lstIntf2.SelectedIndex);

            //to simulate slow response from device 2 we can set for example:
            //dev2.delayread = 2000;

			//example of interface-specific action: setting visa options in case visa is selected for dev1:
           VisaDevice d = dev1 as VisaDevice;
			if (d != null) {
				//for example set some attributes:
               // d.SetAttribute(1, 0);
			}


		
        

			if (dev1 != null)
            {gbox1.Enabled = true;

                     //examples of some settings
             dev1.maxtasks=10;
             dev1.readtimeout=5000;

            dev1.showmessages = true;
            dev1.catchcallbackexceptions = true;

            //dev1.enablepoll = False  //uncomment this if a device does not support polling ("poll timeout" is signalled)

            //dev1.MAVmask = ...;  //set a different mask if your device's status flags do not comply with 488.2

                //uncomment this to use SRQ notifying on device1 
                 // setnotify(dev1); 

            }

            if (dev2 != null)
            { gbox2.Enabled = true;
            
            
            }


			IODevice.ShowDevices();

			btncreate.Enabled = false;
			gboxdev.Enabled = false;
			btndevlist.Enabled = true;


            //uncomment this to use SRQ notifying on device2 
            //  setnotify(dev2); 

		}

		private void btndevlist_Click(System.Object sender, System.EventArgs e)
		{
			IODevice.ShowDevices();
		}


		private void btnq1b_Click(System.Object sender, System.EventArgs e)
		{
            IOQuery q;
            int result;

            btnq1b.Enabled = false;


            result = dev1.QueryBlocking(txtq1b.Text, out q, true);//version with IOQuery parameter
            btnq1b.Enabled = true;
            txtr1astat.Text = "blocking command:'" + q.cmd + "'" + CrLf;
            if (result == 0)
            {
                txtr1b.Text = q.ResponseAsString;

                txtr1astat.Text += "device response time:" + q.timeend.Subtract(q.timestart).TotalSeconds.ToString() + " s" + CrLf;
                txtr1astat.Text += "thread wait time:" + q.timestart.Subtract(q.timecall).TotalSeconds.ToString() + " s" + CrLf;
            }
            else
            {
                txtr1astat.Text += "status: " + result + CrLf;
                txtr1astat.Text += q.errmsg;
            } 

		}


		private void btnq2b_Click(System.Object sender, System.EventArgs e)
		{
			string resp;
			int result;

			btnq2b.Enabled = false;
			result=dev2.QueryBlocking(txtq2b.Text, out resp, true);//simpler version with string parameter
			btnq2b.Enabled = true;

			if (result == 0)
				txtr2b.Text = resp;
		}


		private void btnq1a_Click(System.Object sender, System.EventArgs e)
		{
            if (dev1.PendingTasks(txtq1a.Text) <= 3)  //example of using PendingTasks() method
            { dev1.QueryAsync(txtq1a.Text, cbdev1, true); }
            else { txtr1astat.Text = "already 3 '" + txtq1a.Text + "' commands pending"; }         

		}

		private void btnq2a_Click(System.Object sender, System.EventArgs e)
		{
			dev2.QueryAsync(txtq2a.Text, cbdev2, true);
		}

		//'signature of callback functions:   Public Delegate Sub IOCallback(ByVal q As IOQuery) 


		public void cbdev1(IOQuery q)
		{

			try {

				string s = "async command:'" + q.cmd + "'" + CrLf;
				if (q.status == 0) {
					txtr1a.Text = q.ResponseAsString;
                    s += "device response time:" + q.timeend.Subtract(q.timestart).TotalSeconds.ToString() + " s" + CrLf;
                    s += "thread wait time:" + q.timestart.Subtract(q.timecall).TotalSeconds.ToString() + " s" + CrLf;
		
				} else {
					s += "error: " + q.errcode + CrLf;
				}

				txtr1astat.Text = s;

                //uncomment this to chain on dev2:
                //dev2.QueryAsync(txtq2a.Text, cbdev2, true);

			} catch (Exception ex) {
				txtr1astat.Text = "error in callback function:" + CrLf;
				txtr1astat.Text += ex.Message + CrLf;
				if (ex.InnerException != null)
					txtr1astat.Text += ex.InnerException.Message;
			}
		}



		public void cbdev2(IOQuery q)
		{
			try {
				string s = "async command:'" + q.cmd + "'" + CrLf;
				if (q.status == 0) {
					txtr2a.Text = q.ResponseAsString;
                    s += "device response time:" + q.timeend.Subtract(q.timestart).TotalSeconds.ToString() + " s" + CrLf;
                }
					else
                        {   s += "error: " + q.errcode+CrLf;}
				


				s += "thread wait time:" + q.timestart.Subtract(q.timecall).TotalSeconds.ToString() + " s" + CrLf;

				txtr2astat.Text = s;

                //uncomment this to chain on dev1:
                //dev1.QueryAsync(txtq1a.Text, cbdev1, true);

			} catch (Exception ex) {
				txtr2astat.Text = "error in callback function:" + CrLf;
				txtr2astat.Text += ex.Message + CrLf;
				if (ex.InnerException != null)
					txtr2astat.Text += ex.InnerException.Message;
			}


		}


		private void Formtest_FormClosing(System.Object sender, System.Windows.Forms.FormClosingEventArgs e)
		{
			//shut down gracefully: (may take time)
			IODevice.DisposeAll();

		}

		//suggest correct address format:

        private void formataddr(int interfacenum, TextBox txtaddr)
        {
            string sa = txtaddr.Text.Trim();
            uint v;

            switch (interfacenum)
            {
                
                case 1:
                case 2:

                    if (!uint.TryParse(sa, out v))
                    {
                        txtaddr.Text = "1";

                    }
                    break;
                case 0:
                    if (uint.TryParse(sa, out v))
                    {
                        txtaddr.Text = "GPIB0::" + sa + "::INSTR";
                        
                    }
                    else
                    {
                        txtaddr.Text = "GPIB0::1::INSTR";
                        //or USB format like:  "USB0::xxxx::xxxx::xxxx::INSTR"

                    }
                    break;
                case 3:
                    if (uint.TryParse(sa, out v))
                    {
                        txtaddr.Text = "COM" + sa + ":9600,N,8,1,CRLF";
                    }
                    else
                    {
                        txtaddr.Text = "COM1:9600,N,8,1,CRLF";

                    }
                    break;
                
            }

        }
		private void lstIntf1_SelectedIndexChanged(System.Object sender, System.EventArgs e)
		{
			formataddr(lstIntf1.SelectedIndex, txtaddr1);

		}


		private void lstIntf2_SelectedIndexChanged(System.Object sender, System.EventArgs e)
		{
            formataddr(lstIntf2.SelectedIndex, txtaddr2);

			

		}

	}
}


