namespace LiquesceTray
{
   partial class HiddenFormToAcceptCloseMessage
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HiddenFormToAcceptCloseMessage));
         this.SuspendLayout();
         // 
         // HiddenFormToAcceptCloseMessage
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(140, 52);
         this.ControlBox = false;
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MaximumSize = new System.Drawing.Size(146, 74);
         this.MinimizeBox = false;
         this.Name = "HiddenFormToAcceptCloseMessage";
         this.ShowIcon = false;
         this.ShowInTaskbar = false;
         this.Text = "HiddenFormToAcceptCloseMessage";
         this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.HiddenFormToAcceptCloseMessage_FormClosing);
         this.Load += new System.EventHandler(this.HiddenFormToAcceptCloseMessage_Load);
         this.ResumeLayout(false);

      }

      #endregion
   }
}