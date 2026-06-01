
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
namespace testIODevice
{

	partial class Formtest : System.Windows.Forms.Form
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
            this.gboxdev = new System.Windows.Forms.GroupBox();
            this.Label18 = new System.Windows.Forms.Label();
            this.Label17 = new System.Windows.Forms.Label();
            this.lstIntf2 = new System.Windows.Forms.ComboBox();
            this.lstIntf1 = new System.Windows.Forms.ComboBox();
            this.Label16 = new System.Windows.Forms.Label();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label4 = new System.Windows.Forms.Label();
            this.Label3 = new System.Windows.Forms.Label();
            this.txtaddr1 = new System.Windows.Forms.TextBox();
            this.txtname2 = new System.Windows.Forms.TextBox();
            this.txtaddr2 = new System.Windows.Forms.TextBox();
            this.txtname1 = new System.Windows.Forms.TextBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.btncreate = new System.Windows.Forms.Button();
            this.gbox1 = new System.Windows.Forms.GroupBox();
            this.txtr1astat = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.btnq1a = new System.Windows.Forms.Button();
            this.btnq1b = new System.Windows.Forms.Button();
            this.Label7 = new System.Windows.Forms.Label();
            this.txtr1a = new System.Windows.Forms.TextBox();
            this.txtq1a = new System.Windows.Forms.TextBox();
            this.txtr1b = new System.Windows.Forms.TextBox();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label5 = new System.Windows.Forms.Label();
            this.txtq1b = new System.Windows.Forms.TextBox();
            this.gbox2 = new System.Windows.Forms.GroupBox();
            this.txtr2astat = new System.Windows.Forms.TextBox();
            this.btnq2a = new System.Windows.Forms.Button();
            this.btnq2b = new System.Windows.Forms.Button();
            this.txtq2b = new System.Windows.Forms.TextBox();
            this.txtr2b = new System.Windows.Forms.TextBox();
            this.txtq2a = new System.Windows.Forms.TextBox();
            this.txtr2a = new System.Windows.Forms.TextBox();
            this.Label14 = new System.Windows.Forms.Label();
            this.Label13 = new System.Windows.Forms.Label();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.btndevlist = new System.Windows.Forms.Button();
            this.gboxdev.SuspendLayout();
            this.gbox1.SuspendLayout();
            this.gbox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // gboxdev
            // 
            this.gboxdev.Controls.Add(this.Label18);
            this.gboxdev.Controls.Add(this.Label17);
            this.gboxdev.Controls.Add(this.lstIntf2);
            this.gboxdev.Controls.Add(this.lstIntf1);
            this.gboxdev.Controls.Add(this.Label16);
            this.gboxdev.Controls.Add(this.Label15);
            this.gboxdev.Controls.Add(this.Label4);
            this.gboxdev.Controls.Add(this.Label3);
            this.gboxdev.Controls.Add(this.txtaddr1);
            this.gboxdev.Controls.Add(this.txtname2);
            this.gboxdev.Controls.Add(this.txtaddr2);
            this.gboxdev.Controls.Add(this.txtname1);
            this.gboxdev.Controls.Add(this.Label2);
            this.gboxdev.Controls.Add(this.Label1);
            this.gboxdev.Location = new System.Drawing.Point(1, 1);
            this.gboxdev.Name = "gboxdev";
            this.gboxdev.Size = new System.Drawing.Size(534, 130);
            this.gboxdev.TabIndex = 0;
            this.gboxdev.TabStop = false;
            // 
            // Label18
            // 
            this.Label18.AutoSize = true;
            this.Label18.Location = new System.Drawing.Point(308, 92);
            this.Label18.Name = "Label18";
            this.Label18.Size = new System.Drawing.Size(48, 13);
            this.Label18.TabIndex = 23;
            this.Label18.Text = "interface";
            // 
            // Label17
            // 
            this.Label17.AutoSize = true;
            this.Label17.Location = new System.Drawing.Point(6, 92);
            this.Label17.Name = "Label17";
            this.Label17.Size = new System.Drawing.Size(48, 13);
            this.Label17.TabIndex = 22;
            this.Label17.Text = "interface";
            // 
            // lstIntf2
            // 
            this.lstIntf2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstIntf2.FormattingEnabled = true;
            this.lstIntf2.Location = new System.Drawing.Point(364, 92);
            this.lstIntf2.Name = "lstIntf2";
            this.lstIntf2.Size = new System.Drawing.Size(144, 21);
            this.lstIntf2.TabIndex = 21;
            this.lstIntf2.SelectedIndexChanged += new System.EventHandler(this.lstIntf2_SelectedIndexChanged);
            // 
            // lstIntf1
            // 
            this.lstIntf1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstIntf1.FormattingEnabled = true;
            this.lstIntf1.Location = new System.Drawing.Point(60, 92);
            this.lstIntf1.Name = "lstIntf1";
            this.lstIntf1.Size = new System.Drawing.Size(158, 21);
            this.lstIntf1.TabIndex = 20;
            this.lstIntf1.SelectedIndexChanged += new System.EventHandler(this.lstIntf1_SelectedIndexChanged);
            // 
            // Label16
            // 
            this.Label16.AutoSize = true;
            this.Label16.Location = new System.Drawing.Point(445, 8);
            this.Label16.Name = "Label16";
            this.Label16.Size = new System.Drawing.Size(48, 13);
            this.Label16.TabIndex = 19;
            this.Label16.Text = "device 2";
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Location = new System.Drawing.Point(76, 8);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(51, 13);
            this.Label15.TabIndex = 11;
            this.Label15.Text = "device 1 ";
            // 
            // Label4
            // 
            this.Label4.AutoSize = true;
            this.Label4.Location = new System.Drawing.Point(300, 64);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(47, 13);
            this.Label4.TabIndex = 9;
            this.Label4.Text = "address:";
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Location = new System.Drawing.Point(11, 64);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(44, 13);
            this.Label3.TabIndex = 8;
            this.Label3.Text = "address";
            // 
            // txtaddr1
            // 
            this.txtaddr1.Location = new System.Drawing.Point(60, 61);
            this.txtaddr1.Name = "txtaddr1";
            this.txtaddr1.Size = new System.Drawing.Size(152, 20);
            this.txtaddr1.TabIndex = 7;
            this.txtaddr1.Text = "9";
            // 
            // txtname2
            // 
            this.txtname2.Location = new System.Drawing.Point(364, 29);
            this.txtname2.Name = "txtname2";
            this.txtname2.Size = new System.Drawing.Size(144, 20);
            this.txtname2.TabIndex = 6;
            this.txtname2.Text = "Fluke8845";
            // 
            // txtaddr2
            // 
            this.txtaddr2.Location = new System.Drawing.Point(364, 57);
            this.txtaddr2.Name = "txtaddr2";
            this.txtaddr2.Size = new System.Drawing.Size(144, 20);
            this.txtaddr2.TabIndex = 5;
            this.txtaddr2.Text = "1";
            // 
            // txtname1
            // 
            this.txtname1.Location = new System.Drawing.Point(60, 26);
            this.txtname1.Name = "txtname1";
            this.txtname1.Size = new System.Drawing.Size(152, 20);
            this.txtname1.TabIndex = 2;
            this.txtname1.Text = "Agilent 34410A";
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(308, 29);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(39, 13);
            this.Label2.TabIndex = 1;
            this.Label2.Text = "name :";
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(11, 32);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(39, 13);
            this.Label1.TabIndex = 0;
            this.Label1.Text = " name:";
            // 
            // btncreate
            // 
            this.btncreate.Location = new System.Drawing.Point(546, 33);
            this.btncreate.Name = "btncreate";
            this.btncreate.Size = new System.Drawing.Size(115, 63);
            this.btncreate.TabIndex = 10;
            this.btncreate.Text = "create devices";
            this.btncreate.UseVisualStyleBackColor = true;
            this.btncreate.Click += new System.EventHandler(this.btncreate_Click);
            // 
            // gbox1
            // 
            this.gbox1.Controls.Add(this.txtr1astat);
            this.gbox1.Controls.Add(this.Label9);
            this.gbox1.Controls.Add(this.Label8);
            this.gbox1.Controls.Add(this.btnq1a);
            this.gbox1.Controls.Add(this.btnq1b);
            this.gbox1.Controls.Add(this.Label7);
            this.gbox1.Controls.Add(this.txtr1a);
            this.gbox1.Controls.Add(this.txtq1a);
            this.gbox1.Controls.Add(this.txtr1b);
            this.gbox1.Controls.Add(this.Label6);
            this.gbox1.Controls.Add(this.Label5);
            this.gbox1.Controls.Add(this.txtq1b);
            this.gbox1.Enabled = false;
            this.gbox1.Location = new System.Drawing.Point(1, 164);
            this.gbox1.Name = "gbox1";
            this.gbox1.Size = new System.Drawing.Size(301, 337);
            this.gbox1.TabIndex = 1;
            this.gbox1.TabStop = false;
            // 
            // txtr1astat
            // 
            this.txtr1astat.Location = new System.Drawing.Point(18, 230);
            this.txtr1astat.Multiline = true;
            this.txtr1astat.Name = "txtr1astat";
            this.txtr1astat.Size = new System.Drawing.Size(266, 85);
            this.txtr1astat.TabIndex = 11;
            // 
            // Label9
            // 
            this.Label9.AutoSize = true;
            this.Label9.Location = new System.Drawing.Point(9, 77);
            this.Label9.Name = "Label9";
            this.Label9.Size = new System.Drawing.Size(50, 13);
            this.Label9.TabIndex = 10;
            this.Label9.Text = "response";
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.Location = new System.Drawing.Point(11, 165);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(53, 13);
            this.Label8.TabIndex = 9;
            this.Label8.Text = "command";
            // 
            // btnq1a
            // 
            this.btnq1a.Location = new System.Drawing.Point(221, 164);
            this.btnq1a.Name = "btnq1a";
            this.btnq1a.Size = new System.Drawing.Size(64, 43);
            this.btnq1a.TabIndex = 8;
            this.btnq1a.Text = "query async";
            this.btnq1a.UseVisualStyleBackColor = true;
            this.btnq1a.Click += new System.EventHandler(this.btnq1a_Click);
            // 
            // btnq1b
            // 
            this.btnq1b.Location = new System.Drawing.Point(221, 48);
            this.btnq1b.Name = "btnq1b";
            this.btnq1b.Size = new System.Drawing.Size(64, 46);
            this.btnq1b.TabIndex = 7;
            this.btnq1b.Text = "query blocking";
            this.btnq1b.UseVisualStyleBackColor = true;
            this.btnq1b.Click += new System.EventHandler(this.btnq1b_Click);
            // 
            // Label7
            // 
            this.Label7.AutoSize = true;
            this.Label7.Location = new System.Drawing.Point(9, 194);
            this.Label7.Name = "Label7";
            this.Label7.Size = new System.Drawing.Size(50, 13);
            this.Label7.TabIndex = 6;
            this.Label7.Text = "response";
            // 
            // txtr1a
            // 
            this.txtr1a.Location = new System.Drawing.Point(79, 188);
            this.txtr1a.Name = "txtr1a";
            this.txtr1a.Size = new System.Drawing.Size(129, 20);
            this.txtr1a.TabIndex = 5;
            // 
            // txtq1a
            // 
            this.txtq1a.Location = new System.Drawing.Point(79, 162);
            this.txtq1a.Name = "txtq1a";
            this.txtq1a.Size = new System.Drawing.Size(129, 20);
            this.txtq1a.TabIndex = 4;
            this.txtq1a.Text = "READ?";
            // 
            // txtr1b
            // 
            this.txtr1b.Location = new System.Drawing.Point(79, 74);
            this.txtr1b.Name = "txtr1b";
            this.txtr1b.Size = new System.Drawing.Size(129, 20);
            this.txtr1b.TabIndex = 3;
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.Location = new System.Drawing.Point(108, 16);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(51, 13);
            this.Label6.TabIndex = 2;
            this.Label6.Text = "device 1 ";
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.Location = new System.Drawing.Point(6, 51);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(53, 13);
            this.Label5.TabIndex = 1;
            this.Label5.Text = "command";
            // 
            // txtq1b
            // 
            this.txtq1b.Location = new System.Drawing.Point(79, 48);
            this.txtq1b.Name = "txtq1b";
            this.txtq1b.Size = new System.Drawing.Size(129, 20);
            this.txtq1b.TabIndex = 0;
            this.txtq1b.Text = "READ?";
            // 
            // gbox2
            // 
            this.gbox2.Controls.Add(this.txtr2astat);
            this.gbox2.Controls.Add(this.btnq2a);
            this.gbox2.Controls.Add(this.btnq2b);
            this.gbox2.Controls.Add(this.txtq2b);
            this.gbox2.Controls.Add(this.txtr2b);
            this.gbox2.Controls.Add(this.txtq2a);
            this.gbox2.Controls.Add(this.txtr2a);
            this.gbox2.Controls.Add(this.Label14);
            this.gbox2.Controls.Add(this.Label13);
            this.gbox2.Controls.Add(this.Label12);
            this.gbox2.Controls.Add(this.Label11);
            this.gbox2.Controls.Add(this.Label10);
            this.gbox2.Enabled = false;
            this.gbox2.Location = new System.Drawing.Point(317, 163);
            this.gbox2.Name = "gbox2";
            this.gbox2.Size = new System.Drawing.Size(344, 338);
            this.gbox2.TabIndex = 2;
            this.gbox2.TabStop = false;
            // 
            // txtr2astat
            // 
            this.txtr2astat.Location = new System.Drawing.Point(19, 231);
            this.txtr2astat.Multiline = true;
            this.txtr2astat.Name = "txtr2astat";
            this.txtr2astat.Size = new System.Drawing.Size(266, 85);
            this.txtr2astat.TabIndex = 19;
            // 
            // btnq2a
            // 
            this.btnq2a.Location = new System.Drawing.Point(253, 158);
            this.btnq2a.Name = "btnq2a";
            this.btnq2a.Size = new System.Drawing.Size(64, 43);
            this.btnq2a.TabIndex = 18;
            this.btnq2a.Text = "query async";
            this.btnq2a.UseVisualStyleBackColor = true;
            this.btnq2a.Click += new System.EventHandler(this.btnq2a_Click);
            // 
            // btnq2b
            // 
            this.btnq2b.Location = new System.Drawing.Point(253, 51);
            this.btnq2b.Name = "btnq2b";
            this.btnq2b.Size = new System.Drawing.Size(64, 46);
            this.btnq2b.TabIndex = 17;
            this.btnq2b.Text = "query blocking";
            this.btnq2b.UseVisualStyleBackColor = true;
            this.btnq2b.Click += new System.EventHandler(this.btnq2b_Click);
            // 
            // txtq2b
            // 
            this.txtq2b.Location = new System.Drawing.Point(89, 51);
            this.txtq2b.Name = "txtq2b";
            this.txtq2b.Size = new System.Drawing.Size(129, 20);
            this.txtq2b.TabIndex = 16;
            this.txtq2b.Text = "READ?";
            // 
            // txtr2b
            // 
            this.txtr2b.Location = new System.Drawing.Point(89, 81);
            this.txtr2b.Name = "txtr2b";
            this.txtr2b.Size = new System.Drawing.Size(129, 20);
            this.txtr2b.TabIndex = 15;
            // 
            // txtq2a
            // 
            this.txtq2a.Location = new System.Drawing.Point(89, 158);
            this.txtq2a.Name = "txtq2a";
            this.txtq2a.Size = new System.Drawing.Size(129, 20);
            this.txtq2a.TabIndex = 14;
            this.txtq2a.Text = "READ?";
            // 
            // txtr2a
            // 
            this.txtr2a.Location = new System.Drawing.Point(89, 184);
            this.txtr2a.Name = "txtr2a";
            this.txtr2a.Size = new System.Drawing.Size(129, 20);
            this.txtr2a.TabIndex = 13;
            // 
            // Label14
            // 
            this.Label14.AutoSize = true;
            this.Label14.Location = new System.Drawing.Point(16, 188);
            this.Label14.Name = "Label14";
            this.Label14.Size = new System.Drawing.Size(50, 13);
            this.Label14.TabIndex = 12;
            this.Label14.Text = "response";
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Location = new System.Drawing.Point(16, 81);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(50, 13);
            this.Label13.TabIndex = 11;
            this.Label13.Text = "response";
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.Location = new System.Drawing.Point(13, 162);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(53, 13);
            this.Label12.TabIndex = 5;
            this.Label12.Text = "command";
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.Location = new System.Drawing.Point(16, 51);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(53, 13);
            this.Label11.TabIndex = 4;
            this.Label11.Text = "command";
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Location = new System.Drawing.Point(144, 16);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(48, 13);
            this.Label10.TabIndex = 3;
            this.Label10.Text = "device 2";
            // 
            // btndevlist
            // 
            this.btndevlist.Enabled = false;
            this.btndevlist.Location = new System.Drawing.Point(199, 137);
            this.btndevlist.Name = "btndevlist";
            this.btndevlist.Size = new System.Drawing.Size(184, 24);
            this.btndevlist.TabIndex = 3;
            this.btndevlist.Text = "show  device list";
            this.btndevlist.UseVisualStyleBackColor = true;
            this.btndevlist.Click += new System.EventHandler(this.btndevlist_Click);
            // 
            // Formtest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(673, 513);
            this.Controls.Add(this.btncreate);
            this.Controls.Add(this.btndevlist);
            this.Controls.Add(this.gbox2);
            this.Controls.Add(this.gbox1);
            this.Controls.Add(this.gboxdev);
            this.Name = "Formtest";
            this.Text = "Form1";
            this.gboxdev.ResumeLayout(false);
            this.gboxdev.PerformLayout();
            this.gbox1.ResumeLayout(false);
            this.gbox1.PerformLayout();
            this.gbox2.ResumeLayout(false);
            this.gbox2.PerformLayout();
            this.ResumeLayout(false);

		}
		private System.Windows.Forms.GroupBox gboxdev;
		internal System.Windows.Forms.Label Label4;
		internal System.Windows.Forms.Label Label3;
		internal System.Windows.Forms.TextBox txtaddr1;
		internal System.Windows.Forms.TextBox txtname2;
		internal System.Windows.Forms.TextBox txtaddr2;
		internal System.Windows.Forms.TextBox txtname1;
		internal System.Windows.Forms.Label Label2;
		internal System.Windows.Forms.Label Label1;
		internal System.Windows.Forms.GroupBox gbox1;
		internal System.Windows.Forms.Button btncreate;
		internal System.Windows.Forms.TextBox txtr1a;
		internal System.Windows.Forms.TextBox txtq1a;
		internal System.Windows.Forms.TextBox txtr1b;
		internal System.Windows.Forms.Label Label6;
		internal System.Windows.Forms.Label Label5;
		internal System.Windows.Forms.TextBox txtq1b;
		internal System.Windows.Forms.Label Label9;
		internal System.Windows.Forms.Label Label8;
		internal System.Windows.Forms.Button btnq1a;
		internal System.Windows.Forms.Button btnq1b;
		internal System.Windows.Forms.Label Label7;
		internal System.Windows.Forms.GroupBox gbox2;
		internal System.Windows.Forms.Label Label12;
		internal System.Windows.Forms.Label Label11;
		internal System.Windows.Forms.Label Label10;
		internal System.Windows.Forms.Button btnq2a;
		internal System.Windows.Forms.Button btnq2b;
		internal System.Windows.Forms.TextBox txtq2b;
		internal System.Windows.Forms.TextBox txtr2b;
		internal System.Windows.Forms.TextBox txtq2a;
		internal System.Windows.Forms.TextBox txtr2a;
		internal System.Windows.Forms.Label Label14;
		internal System.Windows.Forms.Label Label13;
		internal System.Windows.Forms.Button btndevlist;
		internal System.Windows.Forms.Label Label16;
		internal System.Windows.Forms.Label Label15;
		internal System.Windows.Forms.ComboBox lstIntf1;
		internal System.Windows.Forms.ComboBox lstIntf2;
		internal System.Windows.Forms.Label Label18;
		internal System.Windows.Forms.Label Label17;
		internal System.Windows.Forms.TextBox txtr1astat;
		internal System.Windows.Forms.TextBox txtr2astat;
		public Formtest()
		{
			FormClosing += Formtest_FormClosing;
			Load += Formtest_Load;
			InitializeComponent();
        }

        #endregion
    }
      
}
