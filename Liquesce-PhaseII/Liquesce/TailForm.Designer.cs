namespace Liquesce
{
   partial class TailForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TailForm));
         this.textBox1 = new System.Windows.Forms.TextBox();
         this.timer1 = new System.Windows.Forms.Timer(this.components);
         this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
         this.contextMenuStrip1.SuspendLayout();
         this.SuspendLayout();
         // 
         // textBox1
         // 
         this.textBox1.BackColor = System.Drawing.SystemColors.Window;
         this.textBox1.CausesValidation = false;
         this.textBox1.ContextMenuStrip = this.contextMenuStrip1;
         this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.textBox1.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.textBox1.Location = new System.Drawing.Point(0, 0);
         this.textBox1.Multiline = true;
         this.textBox1.Name = "textBox1";
         this.textBox1.ReadOnly = true;
         this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
         this.textBox1.Size = new System.Drawing.Size(888, 408);
         this.textBox1.TabIndex = 0;
         this.textBox1.WordWrap = false;
         // 
         // timer1
         // 
         this.timer1.Interval = 250;
         this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
         // 
         // contextMenuStrip1
         // 
         this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3});
         this.contextMenuStrip1.Name = "contextMenuStrip1";
         this.contextMenuStrip1.Size = new System.Drawing.Size(153, 92);
         // 
         // toolStripMenuItem1
         // 
         this.toolStripMenuItem1.Name = "toolStripMenuItem1";
         this.toolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.Delete;
         this.toolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
         this.toolStripMenuItem1.Text = "&Clear";
         this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
         // 
         // toolStripMenuItem2
         // 
         this.toolStripMenuItem2.Name = "toolStripMenuItem2";
         this.toolStripMenuItem2.Size = new System.Drawing.Size(152, 22);
         this.toolStripMenuItem2.Text = "&Freeze";
         this.toolStripMenuItem2.Click += new System.EventHandler(this.toolStripMenuItem2_Click);
         // 
         // toolStripMenuItem3
         // 
         this.toolStripMenuItem3.Name = "toolStripMenuItem3";
         this.toolStripMenuItem3.Size = new System.Drawing.Size(152, 22);
         this.toolStripMenuItem3.Text = "&Resume";
         this.toolStripMenuItem3.Click += new System.EventHandler(this.toolStripMenuItem3_Click);
         // 
         // TailForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(888, 408);
         this.Controls.Add(this.textBox1);
         this.DoubleBuffered = true;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(200, 100);
         this.Name = "TailForm";
         this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
         this.Text = "Tail";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TailForm_FormClosing);
         this.Shown += new System.EventHandler(this.TailForm_Shown);
         this.contextMenuStrip1.ResumeLayout(false);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.TextBox textBox1;
      private System.Windows.Forms.Timer timer1;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
   }
}