namespace ClientManagement
{
   partial class MainForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
         this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
         this.btnConnect = new System.Windows.Forms.Button();
         this.btnSend = new System.Windows.Forms.Button();
         this.btnRefresh = new System.Windows.Forms.Button();
         this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
         this.btnViewLog = new System.Windows.Forms.Button();
         this.serviceController1 = new System.ServiceProcess.ServiceController();
         this.SuspendLayout();
         // 
         // propertyGrid1
         // 
         this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                     | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.propertyGrid1.Location = new System.Drawing.Point(12, 12);
         this.propertyGrid1.Name = "propertyGrid1";
         this.propertyGrid1.Size = new System.Drawing.Size(522, 316);
         this.propertyGrid1.TabIndex = 18;
         // 
         // btnConnect
         // 
         this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnConnect.Location = new System.Drawing.Point(13, 338);
         this.btnConnect.Name = "btnConnect";
         this.btnConnect.Size = new System.Drawing.Size(87, 23);
         this.btnConnect.TabIndex = 19;
         this.btnConnect.Text = "&Connect Test";
         this.toolTip1.SetToolTip(this.btnConnect, "Make sure that the HostName / IP address typed in is actually contactable.");
         this.btnConnect.UseVisualStyleBackColor = true;
         this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
         // 
         // btnSend
         // 
         this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.btnSend.Location = new System.Drawing.Point(447, 338);
         this.btnSend.Name = "btnSend";
         this.btnSend.Size = new System.Drawing.Size(87, 23);
         this.btnSend.TabIndex = 20;
         this.btnSend.Text = "&Send";
         this.toolTip1.SetToolTip(this.btnSend, "Send the Current configuration to the \"Share Enabler Service\".");
         this.btnSend.UseVisualStyleBackColor = true;
         this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
         // 
         // btnRefresh
         // 
         this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
         this.btnRefresh.Enabled = false;
         this.btnRefresh.Location = new System.Drawing.Point(151, 338);
         this.btnRefresh.Name = "btnRefresh";
         this.btnRefresh.Size = new System.Drawing.Size(107, 23);
         this.btnRefresh.TabIndex = 21;
         this.btnRefresh.Text = "&Refresh";
         this.toolTip1.SetToolTip(this.btnRefresh, "Refresh the Share ACL\'s.");
         this.btnRefresh.UseVisualStyleBackColor = true;
         this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
         // 
         // btnViewLog
         // 
         this.btnViewLog.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
         this.btnViewLog.Location = new System.Drawing.Point(309, 338);
         this.btnViewLog.Name = "btnViewLog";
         this.btnViewLog.Size = new System.Drawing.Size(87, 23);
         this.btnViewLog.TabIndex = 22;
         this.btnViewLog.Text = "&View Log";
         this.toolTip1.SetToolTip(this.btnViewLog, "Use the Log view window.");
         this.btnViewLog.UseVisualStyleBackColor = true;
         this.btnViewLog.Click += new System.EventHandler(this.btnViewLog_Click);
         // 
         // serviceController1
         // 
         this.serviceController1.MachineName = "127.0.0.1";
         this.serviceController1.ServiceName = "ClientLiquesceSvc";
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(547, 373);
         this.Controls.Add(this.btnViewLog);
         this.Controls.Add(this.btnRefresh);
         this.Controls.Add(this.btnSend);
         this.Controls.Add(this.btnConnect);
         this.Controls.Add(this.propertyGrid1);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(400, 350);
         this.Name = "MainForm";
         this.Text = "Liquesce Share Enabler Client Management";
         this.Load += new System.EventHandler(this.MainForm_Load);
         this.Shown += new System.EventHandler(this.MainForm_Shown);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.PropertyGrid propertyGrid1;
      private System.Windows.Forms.Button btnConnect;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Button btnSend;
      private System.Windows.Forms.Button btnRefresh;
      private System.ServiceProcess.ServiceController serviceController1;
      private System.Windows.Forms.Button btnViewLog;
   }
}