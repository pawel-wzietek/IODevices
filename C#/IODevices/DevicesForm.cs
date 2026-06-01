
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using IODevices;

namespace IODeviceForms
{
    internal partial class DevicesForm
    {



        private bool enableupdate = true;

        private void updatelist()
        {
            //update gpib list
            string[] sl = null;
           

            bool details = chk_gpibcmd.Checked;


            sl = IODevice.GetDeviceList(details);

            lblstatus.Text = IODevice.statusmsg;


            if (sl != null) { txt_list.Lines = sl; }


        }



        private void Timer1_Tick(System.Object sender, System.EventArgs e)
        {
            if (enableupdate & !IODevice.updated)
            {
                updatelist();
                IODevice.updated = true;

            }

        }


        private void lb_gpib_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
        }

        private void DevicesForm_Load(System.Object sender, System.EventArgs e)
        {
            Timer1.Interval = 50;
            Timer1.Enabled = true;
        }


        private void DevicesForm_Resize(System.Object sender, System.EventArgs e)
        {          

             txt_list.Height = Height - 100;
            txt_list.Width = Width - 40;
            lblstatus.Top = Height - 60;

        }


        public DevicesForm()
        {
            Resize += DevicesForm_Resize;
            Load += DevicesForm_Load;
            InitializeComponent();
        }

        private void txt_list_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            enableupdate = true;
        }

        private void txt_list_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            enableupdate = false;
        }

        private void txt_list_TextChanged(object sender, EventArgs e)
        {

        }
    }

}