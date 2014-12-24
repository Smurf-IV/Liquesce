namespace Liquesce.Tabs
{
   partial class Welcome
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

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.richTextBox1 = new System.Windows.Forms.RichTextBox();
         this.panel1 = new System.Windows.Forms.Panel();
         this.label1 = new System.Windows.Forms.Label();
         this.tsVersion = new System.Windows.Forms.Label();
         this.panel1.SuspendLayout();
         this.SuspendLayout();
         // 
         // richTextBox1
         // 
         this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
         this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.richTextBox1.Location = new System.Drawing.Point(0, 0);
         this.richTextBox1.Name = "richTextBox1";
         this.richTextBox1.ReadOnly = true;
         this.richTextBox1.Size = new System.Drawing.Size(600, 461);
         this.richTextBox1.TabIndex = 1;
         this.richTextBox1.Text = "";
         // 
         // panel1
         // 
         this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
         this.panel1.Controls.Add(this.label1);
         this.panel1.Controls.Add(this.tsVersion);
         this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.panel1.Location = new System.Drawing.Point(0, 461);
         this.panel1.Name = "panel1";
         this.panel1.Size = new System.Drawing.Size(600, 14);
         this.panel1.TabIndex = 2;
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Dock = System.Windows.Forms.DockStyle.Right;
         this.label1.Location = new System.Drawing.Point(460, 0);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(51, 14);
         this.label1.TabIndex = 0;
         this.label1.Text = "Version:";
         // 
         // tsVersion
         // 
         this.tsVersion.AutoSize = true;
         this.tsVersion.Dock = System.Windows.Forms.DockStyle.Right;
         this.tsVersion.Location = new System.Drawing.Point(511, 0);
         this.tsVersion.Name = "tsVersion";
         this.tsVersion.Size = new System.Drawing.Size(89, 14);
         this.tsVersion.TabIndex = 1;
         this.tsVersion.Text = "13.12.9999.21";
         // 
         // Welcome
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.richTextBox1);
         this.Controls.Add(this.panel1);
         this.DoubleBuffered = true;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "Welcome";
         this.Size = new System.Drawing.Size(600, 475);
         this.Load += new System.EventHandler(this.Welcome_Load);
         this.panel1.ResumeLayout(false);
         this.panel1.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.RichTextBox richTextBox1;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.Label tsVersion;
   }
}
