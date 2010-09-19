namespace Liquesce
{
   partial class AdvancedSettings
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdvancedSettings));
         this.ThreadCount = new System.Windows.Forms.NumericUpDown();
         this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
         this.LockTimeoutmSec = new System.Windows.Forms.NumericUpDown();
         this.DokanDebugMode = new System.Windows.Forms.CheckBox();
         this.HoldOffMBytes = new System.Windows.Forms.NumericUpDown();
         this.BufferReadSizeKBytes = new System.Windows.Forms.NumericUpDown();
         this.DelayDokanStartmSec = new System.Windows.Forms.NumericUpDown();
         this.button1 = new System.Windows.Forms.Button();
         this.label2 = new System.Windows.Forms.Label();
         this.label3 = new System.Windows.Forms.Label();
         this.label4 = new System.Windows.Forms.Label();
         this.label5 = new System.Windows.Forms.Label();
         this.label6 = new System.Windows.Forms.Label();
         this.label7 = new System.Windows.Forms.Label();
         this.label8 = new System.Windows.Forms.Label();
         this.label9 = new System.Windows.Forms.Label();
         this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
         this.label1 = new System.Windows.Forms.Label();
         ((System.ComponentModel.ISupportInitialize)(this.ThreadCount)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.LockTimeoutmSec)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.HoldOffMBytes)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.BufferReadSizeKBytes)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.DelayDokanStartmSec)).BeginInit();
         this.SuspendLayout();
         // 
         // ThreadCount
         // 
         this.ThreadCount.Location = new System.Drawing.Point(110, 12);
         this.ThreadCount.Maximum = new decimal(new int[] {
            32,
            0,
            0,
            0});
         this.ThreadCount.Name = "ThreadCount";
         this.ThreadCount.Size = new System.Drawing.Size(70, 22);
         this.ThreadCount.TabIndex = 1;
         this.toolTip1.SetToolTip(this.ThreadCount, "0 is automatic, use 1 for problem finding scenario\'s");
         // 
         // LockTimeoutmSec
         // 
         this.LockTimeoutmSec.Increment = new decimal(new int[] {
            250,
            0,
            0,
            0});
         this.LockTimeoutmSec.Location = new System.Drawing.Point(110, 46);
         this.LockTimeoutmSec.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
         this.LockTimeoutmSec.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
         this.LockTimeoutmSec.Name = "LockTimeoutmSec";
         this.LockTimeoutmSec.Size = new System.Drawing.Size(77, 22);
         this.LockTimeoutmSec.TabIndex = 3;
         this.LockTimeoutmSec.ThousandsSeparator = true;
         this.toolTip1.SetToolTip(this.LockTimeoutmSec, "Useful if you are getting file overwrites in some applications that perform quick" +
                 " creation deletion / creation of files, and multiple threads - Can be set to -1 " +
                 "for infinite");
         this.LockTimeoutmSec.Value = new decimal(new int[] {
            32767,
            0,
            0,
            0});
         // 
         // DokanDebugMode
         // 
         this.DokanDebugMode.AutoSize = true;
         this.DokanDebugMode.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
         this.DokanDebugMode.Enabled = false;
         this.DokanDebugMode.Location = new System.Drawing.Point(19, 77);
         this.DokanDebugMode.Name = "DokanDebugMode";
         this.DokanDebugMode.Size = new System.Drawing.Size(104, 18);
         this.DokanDebugMode.TabIndex = 5;
         this.DokanDebugMode.Text = "&Debug Mode: ";
         this.toolTip1.SetToolTip(this.DokanDebugMode, "Later on will allow Dokan Debug information to be captured into the Service log");
         this.DokanDebugMode.UseVisualStyleBackColor = true;
         // 
         // HoldOffMBytes
         // 
         this.HoldOffMBytes.Location = new System.Drawing.Point(110, 110);
         this.HoldOffMBytes.Maximum = new decimal(new int[] {
            1024000,
            0,
            0,
            0});
         this.HoldOffMBytes.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
         this.HoldOffMBytes.Name = "HoldOffMBytes";
         this.HoldOffMBytes.Size = new System.Drawing.Size(105, 22);
         this.HoldOffMBytes.TabIndex = 7;
         this.HoldOffMBytes.ThousandsSeparator = true;
         this.toolTip1.SetToolTip(this.HoldOffMBytes, "Number of MegaBytes to leave before attempting to use another drive to write to");
         this.HoldOffMBytes.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
         // 
         // BufferReadSizeKBytes
         // 
         this.BufferReadSizeKBytes.Location = new System.Drawing.Point(110, 155);
         this.BufferReadSizeKBytes.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
         this.BufferReadSizeKBytes.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
         this.BufferReadSizeKBytes.Name = "BufferReadSizeKBytes";
         this.BufferReadSizeKBytes.Size = new System.Drawing.Size(77, 22);
         this.BufferReadSizeKBytes.TabIndex = 10;
         this.toolTip1.SetToolTip(this.BufferReadSizeKBytes, "The number of bytes allocated to file reading (4KB is the OS default!)");
         this.BufferReadSizeKBytes.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
         // 
         // DelayDokanStartmSec
         // 
         this.DelayDokanStartmSec.Increment = new decimal(new int[] {
            250,
            0,
            0,
            0});
         this.DelayDokanStartmSec.Location = new System.Drawing.Point(110, 191);
         this.DelayDokanStartmSec.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
         this.DelayDokanStartmSec.Minimum = new decimal(new int[] {
            250,
            0,
            0,
            0});
         this.DelayDokanStartmSec.Name = "DelayDokanStartmSec";
         this.DelayDokanStartmSec.Size = new System.Drawing.Size(105, 22);
         this.DelayDokanStartmSec.TabIndex = 13;
         this.DelayDokanStartmSec.ThousandsSeparator = true;
         this.toolTip1.SetToolTip(this.DelayDokanStartmSec, "This is a Delay Start Service, But this gives the OS a little extra to mount Netw" +
                 "orks and USB devices before attempting to start the Pool driver");
         this.DelayDokanStartmSec.Value = new decimal(new int[] {
            25000,
            0,
            0,
            0});
         // 
         // button1
         // 
         this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.button1.Location = new System.Drawing.Point(270, 237);
         this.button1.Name = "button1";
         this.button1.Size = new System.Drawing.Size(75, 23);
         this.button1.TabIndex = 15;
         this.button1.Text = "Commit";
         this.toolTip1.SetToolTip(this.button1, "Settings above will be written back into the \"Global Config\"");
         this.button1.UseVisualStyleBackColor = true;
         this.button1.Click += new System.EventHandler(this.button1_Click);
         // 
         // label2
         // 
         this.label2.AutoSize = true;
         this.label2.Location = new System.Drawing.Point(15, 48);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(86, 14);
         this.label2.TabIndex = 2;
         this.label2.Text = "&Lock Timeout:";
         // 
         // label3
         // 
         this.label3.AutoSize = true;
         this.label3.Location = new System.Drawing.Point(193, 48);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(22, 14);
         this.label3.TabIndex = 4;
         this.label3.Text = "ms";
         // 
         // label4
         // 
         this.label4.Location = new System.Drawing.Point(18, 102);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(84, 34);
         this.label4.TabIndex = 6;
         this.label4.Text = "&Hold Off Buffer:";
         this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
         // 
         // label5
         // 
         this.label5.AutoSize = true;
         this.label5.Location = new System.Drawing.Point(218, 112);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(46, 14);
         this.label5.TabIndex = 8;
         this.label5.Text = "MBytes";
         // 
         // label6
         // 
         this.label6.Location = new System.Drawing.Point(21, 149);
         this.label6.Name = "label6";
         this.label6.Size = new System.Drawing.Size(80, 31);
         this.label6.TabIndex = 9;
         this.label6.Text = "&Buffer Read Size:";
         this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
         // 
         // label7
         // 
         this.label7.AutoSize = true;
         this.label7.Location = new System.Drawing.Point(189, 157);
         this.label7.Name = "label7";
         this.label7.Size = new System.Drawing.Size(44, 14);
         this.label7.TabIndex = 11;
         this.label7.Text = "KBytes";
         // 
         // label8
         // 
         this.label8.AutoSize = true;
         this.label8.Location = new System.Drawing.Point(24, 193);
         this.label8.Name = "label8";
         this.label8.Size = new System.Drawing.Size(75, 14);
         this.label8.TabIndex = 12;
         this.label8.Text = "Delay &Start :";
         // 
         // label9
         // 
         this.label9.AutoSize = true;
         this.label9.Location = new System.Drawing.Point(218, 193);
         this.label9.Name = "label9";
         this.label9.Size = new System.Drawing.Size(75, 14);
         this.label9.TabIndex = 14;
         this.label9.Text = "milli Seconds";
         // 
         // propertyGrid1
         // 
         this.propertyGrid1.Location = new System.Drawing.Point(196, 2);
         this.propertyGrid1.Name = "propertyGrid1";
         this.propertyGrid1.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
         this.propertyGrid1.Size = new System.Drawing.Size(164, 188);
         this.propertyGrid1.TabIndex = 16;
         this.propertyGrid1.Visible = false;
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Location = new System.Drawing.Point(16, 14);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(87, 14);
         this.label1.TabIndex = 17;
         this.label1.Text = "&Thread Count:";
         // 
         // AdvancedSettings
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(357, 272);
         this.Controls.Add(this.label1);
         this.Controls.Add(this.propertyGrid1);
         this.Controls.Add(this.button1);
         this.Controls.Add(this.label9);
         this.Controls.Add(this.DelayDokanStartmSec);
         this.Controls.Add(this.label8);
         this.Controls.Add(this.label7);
         this.Controls.Add(this.BufferReadSizeKBytes);
         this.Controls.Add(this.label6);
         this.Controls.Add(this.label5);
         this.Controls.Add(this.HoldOffMBytes);
         this.Controls.Add(this.label4);
         this.Controls.Add(this.DokanDebugMode);
         this.Controls.Add(this.label3);
         this.Controls.Add(this.LockTimeoutmSec);
         this.Controls.Add(this.label2);
         this.Controls.Add(this.ThreadCount);
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "AdvancedSettings";
         this.Text = "AdvancedSettings";
         this.Load += new System.EventHandler(this.AdvancedSettings_Load);
         ((System.ComponentModel.ISupportInitialize)(this.ThreadCount)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.LockTimeoutmSec)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.HoldOffMBytes)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.BufferReadSizeKBytes)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.DelayDokanStartmSec)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.NumericUpDown ThreadCount;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.NumericUpDown LockTimeoutmSec;
      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.CheckBox DokanDebugMode;
      private System.Windows.Forms.Label label4;
      private System.Windows.Forms.NumericUpDown HoldOffMBytes;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.Label label6;
      private System.Windows.Forms.NumericUpDown BufferReadSizeKBytes;
      private System.Windows.Forms.Label label7;
      private System.Windows.Forms.Label label8;
      private System.Windows.Forms.NumericUpDown DelayDokanStartmSec;
      private System.Windows.Forms.Label label9;
      private System.Windows.Forms.Button button1;
      private System.Windows.Forms.PropertyGrid propertyGrid1;
      private System.Windows.Forms.Label label1;
   }
}