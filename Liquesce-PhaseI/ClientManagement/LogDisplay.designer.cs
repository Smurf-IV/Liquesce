namespace ClientManagement
{
   partial class LogDisplay
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
         this.textBox1 = new System.Windows.Forms.ListBox();
         this.SuspendLayout();
         // 
         // textBox1
         // 
         this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                     | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
         this.textBox1.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.textBox1.HorizontalScrollbar = true;
         this.textBox1.IntegralHeight = false;
         this.textBox1.ItemHeight = 15;
         this.textBox1.Location = new System.Drawing.Point(4, 4);
         this.textBox1.Margin = new System.Windows.Forms.Padding(2);
         this.textBox1.Name = "textBox1";
         this.textBox1.ScrollAlwaysVisible = true;
         this.textBox1.Size = new System.Drawing.Size(683, 558);
         this.textBox1.TabIndex = 11;
         // 
         // LogDisplay
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(692, 573);
         this.Controls.Add(this.textBox1);
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Margin = new System.Windows.Forms.Padding(2);
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(350, 300);
         this.Name = "LogDisplay";
         this.ShowIcon = false;
         this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
         this.Text = "Log Display";
         this.Shown += new System.EventHandler(this.LogDisplay_Shown);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.ListBox textBox1;
   }
}