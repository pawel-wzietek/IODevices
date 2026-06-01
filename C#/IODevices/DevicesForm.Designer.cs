
using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using IODevices;

namespace IODeviceForms
{
    partial class DevicesForm : System.Windows.Forms.Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.Label1 = new System.Windows.Forms.Label();
            this.Timer1 = new System.Windows.Forms.Timer(this.components);
            this.chk_gpibcmd = new System.Windows.Forms.CheckBox();
            this.lblstatus = new System.Windows.Forms.Label();
            this.txt_list = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(21, 9);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(81, 13);
            this.Label1.TabIndex = 0;
            this.Label1.Text = " Open Devices:";
            // 
            // Timer1
            // 
            this.Timer1.Interval = 200;
            this.Timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // chk_gpibcmd
            // 
            this.chk_gpibcmd.AutoSize = true;
            this.chk_gpibcmd.Checked = true;
            this.chk_gpibcmd.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk_gpibcmd.Location = new System.Drawing.Point(227, 8);
            this.chk_gpibcmd.Name = "chk_gpibcmd";
            this.chk_gpibcmd.Size = new System.Drawing.Size(144, 17);
            this.chk_gpibcmd.TabIndex = 4;
            this.chk_gpibcmd.Text = "show queued commands";
            this.chk_gpibcmd.UseVisualStyleBackColor = true;
            // 
            // lblstatus
            // 
            this.lblstatus.AutoSize = true;
            this.lblstatus.Location = new System.Drawing.Point(12, 265);
            this.lblstatus.Name = "lblstatus";
            this.lblstatus.Size = new System.Drawing.Size(37, 13);
            this.lblstatus.TabIndex = 5;
            this.lblstatus.Text = "          ";
            // 
            // txt_list
            // 
            this.txt_list.Location = new System.Drawing.Point(3, 31);
            this.txt_list.Multiline = true;
            this.txt_list.Name = "txt_list";
            this.txt_list.Size = new System.Drawing.Size(476, 222);
            this.txt_list.TabIndex = 6;
            this.txt_list.TextChanged += new System.EventHandler(this.txt_list_TextChanged);
            this.txt_list.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txt_list_MouseDown);
            this.txt_list.MouseUp += new System.Windows.Forms.MouseEventHandler(this.txt_list_MouseUp);
            // 
            // DevicesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(486, 287);
            this.Controls.Add(this.txt_list);
            this.Controls.Add(this.lblstatus);
            this.Controls.Add(this.chk_gpibcmd);
            this.Controls.Add(this.Label1);
            this.Name = "DevicesForm";
            this.Text = "IO Devices";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        internal System.Windows.Forms.Label Label1;
        internal System.Windows.Forms.Timer Timer1;
        internal System.Windows.Forms.CheckBox chk_gpibcmd;
        internal System.Windows.Forms.Label lblstatus;
        private TextBox txt_list;
    }

        #endregion

}  

