using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using IODevices;


namespace IODeviceForms
{
    internal partial class IOmsgForm : Form
    {
        public IOmsgForm()
        {
            InitializeComponent();
        }
    


    
			//set by calling thread
		private IOQuery query;

        private string CrLf =System.Text.Encoding.UTF8.GetString(new byte[2] { 13, 10 }); //CrLf;
		//called by gpib thread
		public void showit(IOQuery q)
		{

			bool cberr = (q.status & 256)!=0;
			if (cberr)
				q.status -= 256;

			query = q;
			Text = q.device.devname + " error";
			msg1.ForeColor = Color.Red;
			lbl_retry.ForeColor = Color.Red;

			lbl_retry.Visible = true;
			//cmd_abort.Visible = False

			if (query.status > 0) {
				//bit 2 on send(0)/recv(1)
				if ((query.status & 2) == 0) {
					msg1.Text = "error while sending data to " + query.device.devname;
				} else {
					msg1.Text = "error while receiving data from " + query.device.devname;
				}
				//compose info:

				txt.Text = "";


				txt.Text += "address: " + query.device.devaddr;
				txt.Text += CrLf + "command: " + query.cmd;

                if ((query.status & 4) > 0)
                { txt.Text += CrLf + "interface returned error n°" + query.errcode.ToString(); }
				
                txt.Text += CrLf + query.errmsg;
				//if retry:
				if (query.task.retry) {
					lbl_retry.Text = " retrying ...";
					cmd_abort.Visible = true;
					Timer1.Enabled = true;
				} else {
					lbl_retry.Text = "IO operation abandoned";
					cmd_abort.Visible = false;

					Timer1.Enabled = false;
				}

			} else if (cberr) {
				msg1.Text = query.device.devname + " : error in callback ";
				txt.Text = "unhandled exception in user callback function";

				txt.Text += CrLf + "command: " + query.cmd;

				txt.Text += CrLf + query.errmsg;

				lbl_retry.Text = "IO operation completed";
				cmd_abort.Visible = false;

				Timer1.Enabled = false;

			}





			Show();

		}


		private void cmd_abort_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			query.AbortRetry();

			Close();

		}




		private void cmd_ok_Click(System.Object eventSender, System.EventArgs eventArgs)
		{
			Close();

		}


		private void Timer1_Tick(System.Object eventSender, System.EventArgs eventArgs)
		{
			//make it blinking

			if (lbl_retry.Visible) {
				lbl_retry.Visible = false;
			} else {
				lbl_retry.Visible = true;
			}

			this.Show();

		}


		private void cmd_clear_Click(System.Object sender, System.EventArgs e)
		{
		}


		private void cmd_abortall_Click(System.Object sender, System.EventArgs e)
		{
			query.AbortRetry();
			//abort this (blocking or async)

			query.AbortAll();
			//abort all async commands in queue
			Close();
			//Hide()
		}

		private void IOmsgForm_Load(System.Object sender, System.EventArgs e)
		{
			txt.Multiline = true;
		}


		//called by iodevice when shutting down
		public void shutdownmsg(string devname)
		{

			try {
				Text = devname;
				msg1.ForeColor = Color.Red;
				lbl_retry.Visible = false;

				cmd_abort.Visible = false;
				cmd_abortall.Visible = false;
				cmd_ok.Visible = false;

				msg1.Text = "shutting down " + devname + " ...";


				Show();
				WindowState = FormWindowState.Normal;
				BringToFront();


			} catch (Exception ) {
			}



        }

        private void txt_TextChanged(object sender, EventArgs e)
        {

        }

	}

}

