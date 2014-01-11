namespace Liquesce.Tabs
{
   partial class Logging
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
         this.btnZipAndEmail = new Shared.CommandLinkButton();
         this.commandLinkButton2 = new Shared.CommandLinkButton();
         this.commandLinkButton1 = new Shared.CommandLinkButton();
         this.btnServiceTail = new Shared.CommandLinkButton();
         this.SuspendLayout();
         // 
         // btnZipAndEmail
         // 
         this.btnZipAndEmail.ButtonDepress = ((sbyte)(2));
         this.btnZipAndEmail.HighlightFillAlpha = ((byte)(250));
         this.btnZipAndEmail.HighlightFillAlphaMouse = ((byte)(125));
         this.btnZipAndEmail.HighlightFillAlphaNormal = ((byte)(75));
         this.btnZipAndEmail.HighlightWidth = 2F;
         this.btnZipAndEmail.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_Kontact;
         this.btnZipAndEmail.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnZipAndEmail.Location = new System.Drawing.Point(12, 299);
         this.btnZipAndEmail.Name = "btnZipAndEmail";
         this.btnZipAndEmail.Rounding = 10F;
         this.btnZipAndEmail.Size = new System.Drawing.Size(375, 65);
         this.btnZipAndEmail.Subscript = "   Collects created logs and puts the location in the clipboard.";
         this.btnZipAndEmail.TabIndex = 3;
         this.btnZipAndEmail.Text = "Zip Logs ready for sending etc.";
         this.btnZipAndEmail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnZipAndEmail.UseVisualStyleBackColor = true;
         this.btnZipAndEmail.Click += new System.EventHandler(this.btnZipAndEmail_Click);
         // 
         // commandLinkButton2
         // 
         this.commandLinkButton2.ButtonDepress = ((sbyte)(2));
         this.commandLinkButton2.HighlightFillAlpha = ((byte)(250));
         this.commandLinkButton2.HighlightFillAlphaMouse = ((byte)(125));
         this.commandLinkButton2.HighlightFillAlphaNormal = ((byte)(75));
         this.commandLinkButton2.HighlightWidth = 2F;
         this.commandLinkButton2.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_Log;
         this.commandLinkButton2.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.commandLinkButton2.Location = new System.Drawing.Point(12, 204);
         this.commandLinkButton2.Name = "commandLinkButton2";
         this.commandLinkButton2.Rounding = 10F;
         this.commandLinkButton2.Size = new System.Drawing.Size(375, 65);
         this.commandLinkButton2.Subscript = "   Open the Mgt App Log file in a window.";
         this.commandLinkButton2.TabIndex = 2;
         this.commandLinkButton2.Text = "Management Log ...";
         this.commandLinkButton2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.commandLinkButton2.UseVisualStyleBackColor = true;
         this.commandLinkButton2.Click += new System.EventHandler(this.commandLinkButton2_Click);
         // 
         // commandLinkButton1
         // 
         this.commandLinkButton1.ButtonDepress = ((sbyte)(2));
         this.commandLinkButton1.HighlightFillAlpha = ((byte)(250));
         this.commandLinkButton1.HighlightFillAlphaMouse = ((byte)(125));
         this.commandLinkButton1.HighlightFillAlphaNormal = ((byte)(75));
         this.commandLinkButton1.HighlightWidth = 2F;
         this.commandLinkButton1.Image = global::Liquesce.Properties.Resources.Mart_Glaze_Log;
         this.commandLinkButton1.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.commandLinkButton1.Location = new System.Drawing.Point(12, 109);
         this.commandLinkButton1.Name = "commandLinkButton1";
         this.commandLinkButton1.Rounding = 10F;
         this.commandLinkButton1.Size = new System.Drawing.Size(375, 65);
         this.commandLinkButton1.Subscript = "   Open the Service Log file in a window.";
         this.commandLinkButton1.TabIndex = 1;
         this.commandLinkButton1.Text = "Service Log ...";
         this.commandLinkButton1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.commandLinkButton1.UseVisualStyleBackColor = true;
         this.commandLinkButton1.Click += new System.EventHandler(this.commandLinkButton1_Click);
         // 
         // btnServiceTail
         // 
         this.btnServiceTail.ButtonDepress = ((sbyte)(2));
         this.btnServiceTail.HighlightFillAlpha = ((byte)(250));
         this.btnServiceTail.HighlightFillAlphaMouse = ((byte)(125));
         this.btnServiceTail.HighlightFillAlphaNormal = ((byte)(75));
         this.btnServiceTail.HighlightWidth = 2F;
         this.btnServiceTail.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_K_color_edit;
         this.btnServiceTail.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnServiceTail.Location = new System.Drawing.Point(12, 14);
         this.btnServiceTail.Name = "btnServiceTail";
         this.btnServiceTail.Rounding = 10F;
         this.btnServiceTail.Size = new System.Drawing.Size(375, 65);
         this.btnServiceTail.Subscript = "   Open the Service Log file in a \"Tail\" window.";
         this.btnServiceTail.TabIndex = 0;
         this.btnServiceTail.Text = "Service Tail ...";
         this.btnServiceTail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnServiceTail.UseVisualStyleBackColor = true;
         this.btnServiceTail.Click += new System.EventHandler(this.btnServiceTail_Click);
         // 
         // Logging
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.BackColor = System.Drawing.SystemColors.Control;
         this.Controls.Add(this.btnZipAndEmail);
         this.Controls.Add(this.commandLinkButton2);
         this.Controls.Add(this.commandLinkButton1);
         this.Controls.Add(this.btnServiceTail);
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "Logging";
         this.Size = new System.Drawing.Size(600, 475);
         this.Load += new System.EventHandler(this.Logging_Load);
         this.ResumeLayout(false);

      }

      #endregion

      private Shared.CommandLinkButton btnServiceTail;
      private Shared.CommandLinkButton commandLinkButton1;
      private Shared.CommandLinkButton commandLinkButton2;
      private Shared.CommandLinkButton btnZipAndEmail;
   }
}
