namespace LiquesceTray
{
   partial class NotifyIconHandler
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
         this.components = new System.ComponentModel.Container();
         this.rightClickContextMenuNormal = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.managementApp = new System.Windows.Forms.ToolStripMenuItem();
         this.showFreeDiskSpaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.dropperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.repeatLastMessage = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.serviceController1 = new System.ServiceProcess.ServiceController();
         this.timer1 = new System.Windows.Forms.Timer(this.components);
         this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
         this.rightClickContextMenuService = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.stopServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.startServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.managementApp1 = new System.Windows.Forms.ToolStripMenuItem();
         this.showFreeDiskSpaceToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
         this.dropperToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
         this.repeatLastMessage1 = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
         this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
         this.rightClickContextMenuNormal.SuspendLayout();
         this.rightClickContextMenuService.SuspendLayout();
         this.SuspendLayout();
         // 
         // rightClickContextMenuNormal
         // 
         this.rightClickContextMenuNormal.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.rightClickContextMenuNormal.ImageScalingSize = new System.Drawing.Size(24, 24);
         this.rightClickContextMenuNormal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.managementApp,
            this.showFreeDiskSpaceToolStripMenuItem,
            this.dropperToolStripMenuItem,
            this.repeatLastMessage,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
         this.rightClickContextMenuNormal.Name = "rightClickContextMenu";
         this.rightClickContextMenuNormal.Size = new System.Drawing.Size(211, 160);
         this.rightClickContextMenuNormal.Opening += new System.ComponentModel.CancelEventHandler(this.rightClickContextMenu_Opening);
         // 
         // managementApp
         // 
         this.managementApp.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.managementApp.Image = global::LiquesceTray.Properties.Resources.Liquesce;
         this.managementApp.Name = "managementApp";
         this.managementApp.Size = new System.Drawing.Size(210, 30);
         this.managementApp.Text = "&Management App..";
         this.managementApp.Click += new System.EventHandler(this.managementApp_Click);
         // 
         // showFreeDiskSpaceToolStripMenuItem
         // 
         this.showFreeDiskSpaceToolStripMenuItem.Image = global::LiquesceTray.Properties.Resources.free_space;
         this.showFreeDiskSpaceToolStripMenuItem.Name = "showFreeDiskSpaceToolStripMenuItem";
         this.showFreeDiskSpaceToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.showFreeDiskSpaceToolStripMenuItem.Text = "Show Free Disk Space";
         this.showFreeDiskSpaceToolStripMenuItem.Click += new System.EventHandler(this.showFreeDiskSpaceToolStripMenuItem_Click);
         // 
         // dropperToolStripMenuItem
         // 
         this.dropperToolStripMenuItem.Image = global::LiquesceTray.Properties.Resources.drop;
         this.dropperToolStripMenuItem.Name = "dropperToolStripMenuItem";
         this.dropperToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.dropperToolStripMenuItem.Text = "DropZone";
         this.dropperToolStripMenuItem.Click += new System.EventHandler(this.dropperToolStripMenuItem_Click);
         // 
         // repeatLastMessage
         // 
         this.repeatLastMessage.Image = global::LiquesceTray.Properties.Resources.Question;
         this.repeatLastMessage.Name = "repeatLastMessage";
         this.repeatLastMessage.Size = new System.Drawing.Size(210, 30);
         this.repeatLastMessage.Text = "&Repeat Last message...";
         this.repeatLastMessage.Click += new System.EventHandler(this.repeatLastMessage_Click);
         // 
         // toolStripSeparator1
         // 
         this.toolStripSeparator1.Name = "toolStripSeparator1";
         this.toolStripSeparator1.Size = new System.Drawing.Size(207, 6);
         // 
         // exitToolStripMenuItem
         // 
         this.exitToolStripMenuItem.Image = global::LiquesceTray.Properties.Resources.Stop;
         this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
         this.exitToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.exitToolStripMenuItem.Text = "&Exit";
         this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
         // 
         // serviceController1
         // 
         this.serviceController1.ServiceName = "LiquesceSvc";
         // 
         // timer1
         // 
         this.timer1.Interval = 5000;
         this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
         // 
         // notifyIcon1
         // 
         this.notifyIcon1.ContextMenuStrip = this.rightClickContextMenuNormal;
         this.notifyIcon1.Icon = global::LiquesceTray.Properties.Resources.LiquesceIcon;
         this.notifyIcon1.Text = "Liquesce Starting up";
         this.notifyIcon1.Visible = true;
         this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
         this.notifyIcon1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDown);
         // 
         // rightClickContextMenuService
         // 
         this.rightClickContextMenuService.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.rightClickContextMenuService.ImageScalingSize = new System.Drawing.Size(24, 24);
         this.rightClickContextMenuService.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stopServiceToolStripMenuItem,
            this.startServiceToolStripMenuItem,
            this.managementApp1,
            this.showFreeDiskSpaceToolStripMenuItem1,
            this.dropperToolStripMenuItem1,
            this.repeatLastMessage1,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem1});
         this.rightClickContextMenuService.Name = "rightClickContextMenu";
         this.rightClickContextMenuService.Size = new System.Drawing.Size(211, 220);
         this.rightClickContextMenuService.Opening += new System.ComponentModel.CancelEventHandler(this.rightClickContextMenu_Opening);
         // 
         // stopServiceToolStripMenuItem
         // 
         this.stopServiceToolStripMenuItem.Image = global::LiquesceTray.Properties.Resources.Warning;
         this.stopServiceToolStripMenuItem.Name = "stopServiceToolStripMenuItem";
         this.stopServiceToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.stopServiceToolStripMenuItem.Text = "Stop Service";
         this.stopServiceToolStripMenuItem.ToolTipText = "This will send a \"Stop\" signal to the service";
         this.stopServiceToolStripMenuItem.Click += new System.EventHandler(this.stopServiceToolStripMenuItem_Click);
         // 
         // startServiceToolStripMenuItem
         // 
         this.startServiceToolStripMenuItem.Image = global::LiquesceTray.Properties.Resources.Config;
         this.startServiceToolStripMenuItem.Name = "startServiceToolStripMenuItem";
         this.startServiceToolStripMenuItem.Size = new System.Drawing.Size(210, 30);
         this.startServiceToolStripMenuItem.Text = "Start Service";
         this.startServiceToolStripMenuItem.ToolTipText = "This will send a \"Start\" signal to the service";
         this.startServiceToolStripMenuItem.Click += new System.EventHandler(this.startServiceToolStripMenuItem_Click);
         // 
         // managementApp1
         // 
         this.managementApp1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.managementApp1.Image = global::LiquesceTray.Properties.Resources.Liquesce;
         this.managementApp1.Name = "managementApp1";
         this.managementApp1.Size = new System.Drawing.Size(210, 30);
         this.managementApp1.Text = "&Management App..";
         this.managementApp1.Click += new System.EventHandler(this.managementApp_Click);
         // 
         // showFreeDiskSpaceToolStripMenuItem1
         // 
         this.showFreeDiskSpaceToolStripMenuItem1.Image = global::LiquesceTray.Properties.Resources.free_space;
         this.showFreeDiskSpaceToolStripMenuItem1.Name = "showFreeDiskSpaceToolStripMenuItem1";
         this.showFreeDiskSpaceToolStripMenuItem1.Size = new System.Drawing.Size(210, 30);
         this.showFreeDiskSpaceToolStripMenuItem1.Text = "Show Free Disk Space";
         this.showFreeDiskSpaceToolStripMenuItem1.Click += new System.EventHandler(this.showFreeDiskSpaceToolStripMenuItem_Click);
         // 
         // dropperToolStripMenuItem1
         // 
         this.dropperToolStripMenuItem1.Image = global::LiquesceTray.Properties.Resources.drop;
         this.dropperToolStripMenuItem1.Name = "dropperToolStripMenuItem1";
         this.dropperToolStripMenuItem1.Size = new System.Drawing.Size(210, 30);
         this.dropperToolStripMenuItem1.Text = "DropZone";
         this.dropperToolStripMenuItem1.Click += new System.EventHandler(this.dropperToolStripMenuItem_Click);
         // 
         // repeatLastMessage1
         // 
         this.repeatLastMessage1.Image = global::LiquesceTray.Properties.Resources.Question;
         this.repeatLastMessage1.Name = "repeatLastMessage1";
         this.repeatLastMessage1.Size = new System.Drawing.Size(210, 30);
         this.repeatLastMessage1.Text = "&Repeat Last message...";
         this.repeatLastMessage1.Click += new System.EventHandler(this.repeatLastMessage_Click);
         // 
         // toolStripSeparator2
         // 
         this.toolStripSeparator2.Name = "toolStripSeparator2";
         this.toolStripSeparator2.Size = new System.Drawing.Size(207, 6);
         // 
         // exitToolStripMenuItem1
         // 
         this.exitToolStripMenuItem1.Image = global::LiquesceTray.Properties.Resources.Stop;
         this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
         this.exitToolStripMenuItem1.Size = new System.Drawing.Size(210, 30);
         this.exitToolStripMenuItem1.Text = "&Exit";
         this.exitToolStripMenuItem1.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
         // 
         // NotifyIconHandler
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "NotifyIconHandler";
         this.Size = new System.Drawing.Size(175, 162);
         this.rightClickContextMenuNormal.ResumeLayout(false);
         this.rightClickContextMenuService.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.ContextMenuStrip rightClickContextMenuNormal;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      internal System.Windows.Forms.NotifyIcon notifyIcon1;
      private System.Windows.Forms.ToolStripMenuItem managementApp;
      private System.Windows.Forms.ToolStripMenuItem repeatLastMessage;
      private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
      private System.ServiceProcess.ServiceController serviceController1;
      private System.Windows.Forms.Timer timer1;
      private System.Windows.Forms.ToolStripMenuItem showFreeDiskSpaceToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem dropperToolStripMenuItem;
      private System.Windows.Forms.ContextMenuStrip rightClickContextMenuService;
      private System.Windows.Forms.ToolStripMenuItem stopServiceToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem startServiceToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem managementApp1;
      private System.Windows.Forms.ToolStripMenuItem showFreeDiskSpaceToolStripMenuItem1;
      private System.Windows.Forms.ToolStripMenuItem dropperToolStripMenuItem1;
      private System.Windows.Forms.ToolStripMenuItem repeatLastMessage1;
      private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem1;
   }
}
