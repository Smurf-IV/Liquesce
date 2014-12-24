namespace Liquesce.Tabs
{
   partial class Service
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
         this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
         this.pgrsService = new System.Windows.Forms.ProgressBar();
         this.btnStopStart = new Shared.CommandLinkButton();
         this.btnSave = new Shared.CommandLinkButton();
         this.SuspendLayout();
         // 
         // propertyGrid1
         // 
         this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.propertyGrid1.Location = new System.Drawing.Point(3, 3);
         this.propertyGrid1.Name = "propertyGrid1";
         this.propertyGrid1.Size = new System.Drawing.Size(594, 327);
         this.propertyGrid1.TabIndex = 0;
         // 
         // pgrsService
         // 
         this.pgrsService.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.pgrsService.Location = new System.Drawing.Point(384, 430);
         this.pgrsService.Name = "pgrsService";
         this.pgrsService.Size = new System.Drawing.Size(211, 23);
         this.pgrsService.Step = 1;
         this.pgrsService.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
         this.pgrsService.TabIndex = 3;
         this.pgrsService.Visible = false;
         // 
         // btnStopStart
         // 
         this.btnStopStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnStopStart.ButtonDepress = ((sbyte)(2));
         this.btnStopStart.HighlightFillAlpha = ((byte)(250));
         this.btnStopStart.HighlightFillAlphaMouse = ((byte)(125));
         this.btnStopStart.HighlightFillAlphaNormal = ((byte)(75));
         this.btnStopStart.HighlightWidth = 2F;
         this.btnStopStart.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_K_cm_system;
         this.btnStopStart.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnStopStart.Location = new System.Drawing.Point(3, 407);
         this.btnStopStart.Name = "btnStopStart";
         this.btnStopStart.Rounding = 10F;
         this.btnStopStart.Size = new System.Drawing.Size(375, 65);
         this.btnStopStart.Subscript = "   Stop, then start the service, to use the last saved settings.";
         this.btnStopStart.TabIndex = 2;
         this.btnStopStart.Text = "Restart The Service";
         this.btnStopStart.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnStopStart.UseVisualStyleBackColor = true;
         this.btnStopStart.Click += new System.EventHandler(this.btnStopStart_Click);
         // 
         // btnSave
         // 
         this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnSave.ButtonDepress = ((sbyte)(2));
         this.btnSave.HighlightFillAlpha = ((byte)(250));
         this.btnSave.HighlightFillAlphaMouse = ((byte)(125));
         this.btnSave.HighlightFillAlphaNormal = ((byte)(75));
         this.btnSave.HighlightWidth = 2F;
         this.btnSave.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_3floppy_mount;
         this.btnSave.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnSave.Location = new System.Drawing.Point(3, 336);
         this.btnSave.Name = "btnSave";
         this.btnSave.Rounding = 10F;
         this.btnSave.Size = new System.Drawing.Size(375, 65);
         this.btnSave.Subscript = "   Save the config file settings including all the mount points.";
         this.btnSave.TabIndex = 1;
         this.btnSave.Text = "Save All Settings";
         this.btnSave.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnSave.UseVisualStyleBackColor = true;
         this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
         // 
         // Service
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.pgrsService);
         this.Controls.Add(this.btnStopStart);
         this.Controls.Add(this.propertyGrid1);
         this.Controls.Add(this.btnSave);
         this.DoubleBuffered = true;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "Service";
         this.Size = new System.Drawing.Size(600, 475);
         this.Load += new System.EventHandler(this.Service_Load);
         this.ResumeLayout(false);

      }

      #endregion

      private Shared.CommandLinkButton btnSave;
      private System.Windows.Forms.PropertyGrid propertyGrid1;
      private Shared.CommandLinkButton btnStopStart;
      private System.Windows.Forms.ProgressBar pgrsService;
   }
}
