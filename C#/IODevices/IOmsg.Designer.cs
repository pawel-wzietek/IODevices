namespace IODeviceForms
{
    partial class IOmsgForm
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
            this.msg1 = new System.Windows.Forms.Label();
            this.lbl_retry = new System.Windows.Forms.Label();
            this.txt = new System.Windows.Forms.TextBox();
            this.Timer1 = new System.Windows.Forms.Timer(this.components);
            this.cmd_abort = new System.Windows.Forms.Button();
            this.cmd_abortall = new System.Windows.Forms.Button();
            this.cmd_ok = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // msg1
            // 
            this.msg1.BackColor = System.Drawing.SystemColors.Window;
            this.msg1.Cursor = System.Windows.Forms.Cursors.Default;
            this.msg1.ForeColor = System.Drawing.SystemColors.WindowText;
            this.msg1.Location = new System.Drawing.Point(3, 9);
            this.msg1.Name = "msg1";
            this.msg1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.msg1.Size = new System.Drawing.Size(331, 18);
            this.msg1.TabIndex = 0;
            this.msg1.Text = " error while sending data to ";
            // 
            // lbl_retry
            // 
            this.lbl_retry.BackColor = System.Drawing.SystemColors.Window;
            this.lbl_retry.Cursor = System.Windows.Forms.Cursors.Default;
            this.lbl_retry.ForeColor = System.Drawing.SystemColors.WindowText;
            this.lbl_retry.Location = new System.Drawing.Point(2, 118);
            this.lbl_retry.Name = "lbl_retry";
            this.lbl_retry.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lbl_retry.Size = new System.Drawing.Size(311, 29);
            this.lbl_retry.TabIndex = 7;
            this.lbl_retry.Text = "retrying ...";
            this.lbl_retry.Visible = false;
            // 
            // txt
            // 
            this.txt.Location = new System.Drawing.Point(6, 30);
            this.txt.Multiline = true;
            this.txt.Name = "txt";
            this.txt.Size = new System.Drawing.Size(328, 85);
            this.txt.TabIndex = 8;
            this.txt.TextChanged += new System.EventHandler(this.txt_TextChanged);
            // 
            // Timer1
            // 
            this.Timer1.Interval = 500;
            this.Timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // cmd_abort
            // 
            this.cmd_abort.BackColor = System.Drawing.SystemColors.Control;
            this.cmd_abort.Cursor = System.Windows.Forms.Cursors.Default;
            this.cmd_abort.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cmd_abort.Location = new System.Drawing.Point(6, 150);
            this.cmd_abort.Name = "cmd_abort";
            this.cmd_abort.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.cmd_abort.Size = new System.Drawing.Size(73, 36);
            this.cmd_abort.TabIndex = 3;
            this.cmd_abort.Text = "abort retry";
            this.cmd_abort.UseVisualStyleBackColor = false;
            this.cmd_abort.Click += new System.EventHandler(this.cmd_abort_Click);
            // 
            // cmd_abortall
            // 
            this.cmd_abortall.BackColor = System.Drawing.SystemColors.Control;
            this.cmd_abortall.Location = new System.Drawing.Point(100, 147);
            this.cmd_abortall.Name = "cmd_abortall";
            this.cmd_abortall.Size = new System.Drawing.Size(139, 39);
            this.cmd_abortall.TabIndex = 9;
            this.cmd_abortall.Text = "abort all pending commands on this device";
            this.cmd_abortall.UseVisualStyleBackColor = false;
            this.cmd_abortall.Click += new System.EventHandler(this.cmd_abortall_Click);
            // 
            // cmd_ok
            // 
            this.cmd_ok.BackColor = System.Drawing.SystemColors.Control;
            this.cmd_ok.Cursor = System.Windows.Forms.Cursors.Default;
            this.cmd_ok.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cmd_ok.Location = new System.Drawing.Point(270, 153);
            this.cmd_ok.Name = "cmd_ok";
            this.cmd_ok.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.cmd_ok.Size = new System.Drawing.Size(73, 33);
            this.cmd_ok.TabIndex = 2;
            this.cmd_ok.Text = "Close";
            this.cmd_ok.UseVisualStyleBackColor = false;
            this.cmd_ok.Click += new System.EventHandler(this.cmd_ok_Click);
            // 
            // IOmsgForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(346, 194);
            this.Controls.Add(this.cmd_abortall);
            this.Controls.Add(this.txt);
            this.Controls.Add(this.cmd_abort);
            this.Controls.Add(this.cmd_ok);
            this.Controls.Add(this.lbl_retry);
            this.Controls.Add(this.msg1);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.ForeColor = System.Drawing.SystemColors.WindowText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Location = new System.Drawing.Point(121, 116);
            this.MaximizeBox = false;
            this.Name = "IOmsgForm";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "IO Device error message";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label msg1;
        private System.Windows.Forms.Label lbl_retry;
        private System.Windows.Forms.TextBox txt;
        private System.Windows.Forms.Timer Timer1;
        private System.Windows.Forms.Button cmd_abort;
        private System.Windows.Forms.Button cmd_abortall;
        private System.Windows.Forms.Button cmd_ok;
    }
}