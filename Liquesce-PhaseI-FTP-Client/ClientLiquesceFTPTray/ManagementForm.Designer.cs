namespace ClientLiquesceFTPTray
{
   partial class ManagementForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManagementForm));
         this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
         this.btnConnect = new System.Windows.Forms.Button();
         this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
         this.btnSave = new System.Windows.Forms.Button();
         this.btnLogView = new System.Windows.Forms.Button();
         this.SuspendLayout();
         // 
         // propertyGrid1
         // 
         this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                     | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.propertyGrid1.Location = new System.Drawing.Point(14, 13);
         this.propertyGrid1.Name = "propertyGrid1";
         this.propertyGrid1.Size = new System.Drawing.Size(609, 340);
         this.propertyGrid1.TabIndex = 18;
         // 
         // btnConnect
         // 
         this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnConnect.Location = new System.Drawing.Point(15, 364);
         this.btnConnect.Name = "btnConnect";
         this.btnConnect.Size = new System.Drawing.Size(101, 25);
         this.btnConnect.TabIndex = 19;
         this.btnConnect.Text = "&Connect Test";
         this.toolTip1.SetToolTip(this.btnConnect, "Make sure that the HostName / IP address typed in is actually contactable.");
         this.btnConnect.UseVisualStyleBackColor = true;
         this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
         // 
         // btnSave
         // 
         this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnSave.Enabled = false;
         this.btnSave.Location = new System.Drawing.Point(521, 364);
         this.btnSave.Name = "btnSave";
         this.btnSave.Size = new System.Drawing.Size(101, 25);
         this.btnSave.TabIndex = 20;
         this.btnSave.Text = "&Save";
         this.toolTip1.SetToolTip(this.btnSave, "Save and use the details of this mapping.");
         this.btnSave.UseVisualStyleBackColor = true;
         this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
         // 
         // btnLogView
         // 
         this.btnLogView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnLogView.Location = new System.Drawing.Point(152, 364);
         this.btnLogView.Name = "btnLogView";
         this.btnLogView.Size = new System.Drawing.Size(101, 25);
         this.btnLogView.TabIndex = 21;
         this.btnLogView.Text = "Client &Log View";
         this.toolTip1.SetToolTip(this.btnLogView, "View the NLog output.");
         this.btnLogView.UseVisualStyleBackColor = true;
         this.btnLogView.Click += new System.EventHandler(this.btnLogView_Click);
         // 
         // ManagementForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(638, 402);
         this.Controls.Add(this.btnLogView);
         this.Controls.Add(this.btnSave);
         this.Controls.Add(this.btnConnect);
         this.Controls.Add(this.propertyGrid1);
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(464, 374);
         this.Name = "ManagementForm";
         this.Text = "Liquesce Share Enabler Client Management";
         this.Load += new System.EventHandler(this.ManagementForm_Load);
         this.Shown += new System.EventHandler(this.ManagementForm_Shown);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.PropertyGrid propertyGrid1;
      private System.Windows.Forms.Button btnConnect;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Button btnSave;
      private System.Windows.Forms.Button btnLogView;
   }
}