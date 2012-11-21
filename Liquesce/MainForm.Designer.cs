namespace Liquesce
{
   sealed partial class MainForm
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
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.driveAndDirTreeView = new System.Windows.Forms.TreeView();
         this.imageListUnits = new System.Windows.Forms.ImageList(this.components);
         this.label1 = new System.Windows.Forms.Label();
         this.splitContainer2 = new System.Windows.Forms.SplitContainer();
         this.splitContainer3 = new System.Windows.Forms.SplitContainer();
         this.mergeList = new System.Windows.Forms.TreeView();
         this.mergeListContext = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.VolumeLabel = new System.Windows.Forms.TextBox();
         this.MountPoint = new System.Windows.Forms.ComboBox();
         this.label5 = new System.Windows.Forms.Label();
         this.label4 = new System.Windows.Forms.Label();
         this.DelayCreation = new System.Windows.Forms.NumericUpDown();
         this.label2 = new System.Windows.Forms.Label();
         this.expectedTreeView = new System.Windows.Forms.TreeView();
         this.refreshExpected = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.refreshExpectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.label3 = new System.Windows.Forms.Label();
         this.progressBar1 = new System.Windows.Forms.ProgressBar();
         this.mergeListContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.menuStrip1 = new System.Windows.Forms.MenuStrip();
         this.commitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.logsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.serviceLogViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.userLogViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.advancedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.globalConfigSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.currentSharesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
         this.FillExpectedLayoutWorker = new System.ComponentModel.BackgroundWorker();
         this.serviceController1 = new System.ServiceProcess.ServiceController();
         this.versionNumberToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
         this.splitContainer2.Panel1.SuspendLayout();
         this.splitContainer2.Panel2.SuspendLayout();
         this.splitContainer2.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
         this.splitContainer3.Panel1.SuspendLayout();
         this.splitContainer3.Panel2.SuspendLayout();
         this.splitContainer3.SuspendLayout();
         this.mergeListContext.SuspendLayout();
         this.groupBox1.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.DelayCreation)).BeginInit();
         this.refreshExpected.SuspendLayout();
         this.menuStrip1.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer1
         // 
         this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer1.Location = new System.Drawing.Point(0, 24);
         this.splitContainer1.Name = "splitContainer1";
         // 
         // splitContainer1.Panel1
         // 
         this.splitContainer1.Panel1.Controls.Add(this.driveAndDirTreeView);
         this.splitContainer1.Panel1.Controls.Add(this.label1);
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
         this.splitContainer1.Size = new System.Drawing.Size(770, 465);
         this.splitContainer1.SplitterDistance = 256;
         this.splitContainer1.SplitterWidth = 5;
         this.splitContainer1.TabIndex = 0;
         // 
         // driveAndDirTreeView
         // 
         this.driveAndDirTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
         this.driveAndDirTreeView.FullRowSelect = true;
         this.driveAndDirTreeView.ImageIndex = 0;
         this.driveAndDirTreeView.ImageList = this.imageListUnits;
         this.driveAndDirTreeView.Location = new System.Drawing.Point(0, 17);
         this.driveAndDirTreeView.Name = "driveAndDirTreeView";
         this.driveAndDirTreeView.SelectedImageIndex = 0;
         this.driveAndDirTreeView.Size = new System.Drawing.Size(256, 448);
         this.driveAndDirTreeView.TabIndex = 0;
         this.toolTip1.SetToolTip(this.driveAndDirTreeView, "Drag from here and drop in the middle");
         this.driveAndDirTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.driveAndDirTreeView_BeforeExpand);
         this.driveAndDirTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.driveAndDirTreeView_MouseDown);
         // 
         // imageListUnits
         // 
         this.imageListUnits.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
         this.imageListUnits.ImageSize = new System.Drawing.Size(16, 16);
         this.imageListUnits.TransparentColor = System.Drawing.Color.Transparent;
         // 
         // label1
         // 
         this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label1.Dock = System.Windows.Forms.DockStyle.Top;
         this.label1.Location = new System.Drawing.Point(0, 0);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(256, 17);
         this.label1.TabIndex = 1;
         this.label1.Text = "This file system:-";
         // 
         // splitContainer2
         // 
         this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer2.Location = new System.Drawing.Point(0, 0);
         this.splitContainer2.Name = "splitContainer2";
         // 
         // splitContainer2.Panel1
         // 
         this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
         this.splitContainer2.Panel1.Controls.Add(this.label2);
         // 
         // splitContainer2.Panel2
         // 
         this.splitContainer2.Panel2.Controls.Add(this.expectedTreeView);
         this.splitContainer2.Panel2.Controls.Add(this.label3);
         this.splitContainer2.Panel2.Controls.Add(this.progressBar1);
         this.splitContainer2.Size = new System.Drawing.Size(509, 465);
         this.splitContainer2.SplitterDistance = 247;
         this.splitContainer2.SplitterWidth = 5;
         this.splitContainer2.TabIndex = 0;
         // 
         // splitContainer3
         // 
         this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer3.Location = new System.Drawing.Point(0, 17);
         this.splitContainer3.Name = "splitContainer3";
         this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainer3.Panel1
         // 
         this.splitContainer3.Panel1.Controls.Add(this.mergeList);
         // 
         // splitContainer3.Panel2
         // 
         this.splitContainer3.Panel2.Controls.Add(this.groupBox1);
         this.splitContainer3.Panel2.Controls.Add(this.MountPoint);
         this.splitContainer3.Panel2.Controls.Add(this.label5);
         this.splitContainer3.Panel2.Controls.Add(this.label4);
         this.splitContainer3.Panel2.Controls.Add(this.DelayCreation);
         this.splitContainer3.Size = new System.Drawing.Size(247, 448);
         this.splitContainer3.SplitterDistance = 344;
         this.splitContainer3.TabIndex = 7;
         // 
         // mergeList
         // 
         this.mergeList.AllowDrop = true;
         this.mergeList.ContextMenuStrip = this.mergeListContext;
         this.mergeList.Dock = System.Windows.Forms.DockStyle.Fill;
         this.mergeList.FullRowSelect = true;
         this.mergeList.ImageIndex = 0;
         this.mergeList.ImageList = this.imageListUnits;
         this.mergeList.ItemHeight = 14;
         this.mergeList.Location = new System.Drawing.Point(0, 0);
         this.mergeList.Name = "mergeList";
         this.mergeList.SelectedImageIndex = 0;
         this.mergeList.Size = new System.Drawing.Size(247, 344);
         this.mergeList.TabIndex = 3;
         this.toolTip1.SetToolTip(this.mergeList, "Drop the Entries here");
         this.mergeList.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.mergeList_NodeMouseClick);
         this.mergeList.DragDrop += new System.Windows.Forms.DragEventHandler(this.mergeList_DragDrop);
         this.mergeList.DragOver += new System.Windows.Forms.DragEventHandler(this.mergeList_DragOver);
         this.mergeList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.mergeList_KeyUp);
         this.mergeList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mergeList_MouseDown);
         // 
         // mergeListContext
         // 
         this.mergeListContext.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.mergeListContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem});
         this.mergeListContext.Name = "mergeListContext";
         this.mergeListContext.Size = new System.Drawing.Size(147, 26);
         this.mergeListContext.Click += new System.EventHandler(this.mergeListContextMenuItem_Click);
         // 
         // deleteToolStripMenuItem
         // 
         this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
         this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
         this.deleteToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
         this.deleteToolStripMenuItem.Text = "&Delete";
         this.deleteToolStripMenuItem.ToolTipText = "Delete the current selected item.";
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.VolumeLabel);
         this.groupBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.groupBox1.Location = new System.Drawing.Point(0, 56);
         this.groupBox1.Margin = new System.Windows.Forms.Padding(0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(247, 44);
         this.groupBox1.TabIndex = 4;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "&Volume Label :";
         // 
         // VolumeLabel
         // 
         this.VolumeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.VolumeLabel.Location = new System.Drawing.Point(7, 18);
         this.VolumeLabel.MaxLength = 32;
         this.VolumeLabel.Name = "VolumeLabel";
         this.VolumeLabel.Size = new System.Drawing.Size(234, 22);
         this.VolumeLabel.TabIndex = 0;
         this.toolTip1.SetToolTip(this.VolumeLabel, "Label that will be visible in Windows explorer");
         this.VolumeLabel.Validated += new System.EventHandler(this.VolumeLabel_Validated);
         // 
         // MountPoint
         // 
         this.MountPoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.MountPoint.FormattingEnabled = true;
         this.MountPoint.Items.AddRange(new object[] {
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "J",
            "K",
            "L",
            "M",
            "N",
            "O",
            "P",
            "Q",
            "R",
            "S",
            "T",
            "U",
            "V",
            "W",
            "X",
            "Y",
            "Z"});
         this.MountPoint.Location = new System.Drawing.Point(87, 30);
         this.MountPoint.Name = "MountPoint";
         this.MountPoint.Size = new System.Drawing.Size(154, 22);
         this.MountPoint.Sorted = true;
         this.MountPoint.TabIndex = 3;
         this.toolTip1.SetToolTip(this.MountPoint, "Drive letter to be used for the new volume");
         this.MountPoint.TextChanged += new System.EventHandler(this.MountPoint_TextChanged);
         // 
         // label5
         // 
         this.label5.AutoSize = true;
         this.label5.Location = new System.Drawing.Point(4, 33);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(77, 14);
         this.label5.TabIndex = 2;
         this.label5.Text = "Drive &Mount:";
         // 
         // label4
         // 
         this.label4.AutoSize = true;
         this.label4.Location = new System.Drawing.Point(4, 4);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(118, 14);
         this.label4.TabIndex = 1;
         this.label4.Text = "&Delay Creation (ms):";
         // 
         // DelayCreation
         // 
         this.DelayCreation.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
         this.DelayCreation.Location = new System.Drawing.Point(140, 2);
         this.DelayCreation.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
         this.DelayCreation.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
         this.DelayCreation.Name = "DelayCreation";
         this.DelayCreation.Size = new System.Drawing.Size(101, 22);
         this.DelayCreation.TabIndex = 0;
         this.DelayCreation.ThousandsSeparator = true;
         this.toolTip1.SetToolTip(this.DelayCreation, "Range 0 <-> 10000000\r\nThis is a Delay Start Service, But this gives the OS a li" +
                 "ttle extra to mount Networks and USB devices before attempting to start the Pool" +
                 " driver.\r\n");
         this.DelayCreation.Value = new decimal(new int[] {
            250,
            0,
            0,
            0});
         // 
         // label2
         // 
         this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label2.Dock = System.Windows.Forms.DockStyle.Top;
         this.label2.Location = new System.Drawing.Point(0, 0);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(247, 17);
         this.label2.TabIndex = 2;
         this.label2.Text = "Merge points:-";
         // 
         // expectedTreeView
         // 
         this.expectedTreeView.ContextMenuStrip = this.refreshExpected;
         this.expectedTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
         this.expectedTreeView.FullRowSelect = true;
         this.expectedTreeView.ImageIndex = 0;
         this.expectedTreeView.ImageList = this.imageListUnits;
         this.expectedTreeView.Location = new System.Drawing.Point(0, 17);
         this.expectedTreeView.Name = "expectedTreeView";
         this.expectedTreeView.SelectedImageIndex = 0;
         this.expectedTreeView.ShowNodeToolTips = true;
         this.expectedTreeView.Size = new System.Drawing.Size(257, 425);
         this.expectedTreeView.TabIndex = 0;
         this.toolTip1.SetToolTip(this.expectedTreeView, "Expand to see if any duplicates have been found");
         this.expectedTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.expectedTreeView_BeforeExpand);
         // 
         // refreshExpected
         // 
         this.refreshExpected.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.refreshExpected.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshExpectedToolStripMenuItem});
         this.refreshExpected.Name = "refreshExpected";
         this.refreshExpected.Size = new System.Drawing.Size(204, 26);
         // 
         // refreshExpectedToolStripMenuItem
         // 
         this.refreshExpectedToolStripMenuItem.Name = "refreshExpectedToolStripMenuItem";
         this.refreshExpectedToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
         this.refreshExpectedToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
         this.refreshExpectedToolStripMenuItem.Text = "&Refresh Expected";
         this.refreshExpectedToolStripMenuItem.Click += new System.EventHandler(this.refreshExpectedToolStripMenuItem_Click);
         // 
         // label3
         // 
         this.label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label3.Dock = System.Windows.Forms.DockStyle.Top;
         this.label3.Location = new System.Drawing.Point(0, 0);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(257, 17);
         this.label3.TabIndex = 2;
         this.label3.Text = "Expected layout :-";
         // 
         // progressBar1
         // 
         this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.progressBar1.ForeColor = System.Drawing.Color.LawnGreen;
         this.progressBar1.Location = new System.Drawing.Point(0, 442);
         this.progressBar1.Name = "progressBar1";
         this.progressBar1.Size = new System.Drawing.Size(257, 23);
         this.progressBar1.Step = 5;
         this.progressBar1.TabIndex = 4;
         // 
         // mergeListContextMenuItem
         // 
         this.mergeListContextMenuItem.Name = "mergeListContextMenuItem";
         this.mergeListContextMenuItem.Size = new System.Drawing.Size(32, 19);
         // 
         // menuStrip1
         // 
         this.menuStrip1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.commitToolStripMenuItem,
            this.logsToolStripMenuItem,
            this.advancedToolStripMenuItem,
            this.versionNumberToolStripMenuItem});
         this.menuStrip1.Location = new System.Drawing.Point(0, 0);
         this.menuStrip1.Name = "menuStrip1";
         this.menuStrip1.ShowItemToolTips = true;
         this.menuStrip1.Size = new System.Drawing.Size(770, 24);
         this.menuStrip1.TabIndex = 5;
         this.menuStrip1.Text = "menuStrip1";
         // 
         // commitToolStripMenuItem
         // 
         this.commitToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.commitToolStripMenuItem.Enabled = false;
         this.commitToolStripMenuItem.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.commitToolStripMenuItem.Name = "commitToolStripMenuItem";
         this.commitToolStripMenuItem.Size = new System.Drawing.Size(123, 20);
         this.commitToolStripMenuItem.Text = "&Send Configuration";
         this.commitToolStripMenuItem.ToolTipText = "Send / Commit the stored information to the service";
         this.commitToolStripMenuItem.Click += new System.EventHandler(this.commitToolStripMenuItem_Click);
         // 
         // logsToolStripMenuItem
         // 
         this.logsToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.logsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serviceLogViewToolStripMenuItem,
            this.userLogViewToolStripMenuItem});
         this.logsToolStripMenuItem.Name = "logsToolStripMenuItem";
         this.logsToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
         this.logsToolStripMenuItem.Text = "&Logs";
         // 
         // serviceLogViewToolStripMenuItem
         // 
         this.serviceLogViewToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.serviceLogViewToolStripMenuItem.Name = "serviceLogViewToolStripMenuItem";
         this.serviceLogViewToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
         this.serviceLogViewToolStripMenuItem.Text = "&Service Log View...";
         this.serviceLogViewToolStripMenuItem.Click += new System.EventHandler(this.serviceLogViewToolStripMenuItem_Click);
         // 
         // userLogViewToolStripMenuItem
         // 
         this.userLogViewToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.userLogViewToolStripMenuItem.Name = "userLogViewToolStripMenuItem";
         this.userLogViewToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
         this.userLogViewToolStripMenuItem.Text = "&User Log View...";
         this.userLogViewToolStripMenuItem.Click += new System.EventHandler(this.userLogViewToolStripMenuItem_Click);
         // 
         // advancedToolStripMenuItem
         // 
         this.advancedToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.advancedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.globalConfigSettingsToolStripMenuItem,
            this.currentSharesToolStripMenuItem});
         this.advancedToolStripMenuItem.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.advancedToolStripMenuItem.Name = "advancedToolStripMenuItem";
         this.advancedToolStripMenuItem.Size = new System.Drawing.Size(73, 20);
         this.advancedToolStripMenuItem.Text = "&Advanced";
         this.advancedToolStripMenuItem.ToolTipText = "Advanced options to aid in fault finding and optimisation";
         // 
         // globalConfigSettingsToolStripMenuItem
         // 
         this.globalConfigSettingsToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.globalConfigSettingsToolStripMenuItem.Name = "globalConfigSettingsToolStripMenuItem";
         this.globalConfigSettingsToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
         this.globalConfigSettingsToolStripMenuItem.Text = "&Global Config Settings...";
         this.globalConfigSettingsToolStripMenuItem.ToolTipText = "Access to the settings that control access to the OS and the Dokan Driver";
         this.globalConfigSettingsToolStripMenuItem.Click += new System.EventHandler(this.globalConfigSettingsToolStripMenuItem_Click);
         // 
         // currentSharesToolStripMenuItem
         // 
         this.currentSharesToolStripMenuItem.Enabled = false;
         this.currentSharesToolStripMenuItem.Name = "currentSharesToolStripMenuItem";
         this.currentSharesToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
         this.currentSharesToolStripMenuItem.Text = "&Current Shares...";
         this.currentSharesToolStripMenuItem.ToolTipText = "Requires this to be running at the administrator level";
         this.currentSharesToolStripMenuItem.Click += new System.EventHandler(this.currentSharesToolStripMenuItem_Click);
         // 
         // FillExpectedLayoutWorker
         // 
         this.FillExpectedLayoutWorker.WorkerReportsProgress = true;
         this.FillExpectedLayoutWorker.WorkerSupportsCancellation = true;
         this.FillExpectedLayoutWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.FillExpectedLayoutWorker_DoWork);
         this.FillExpectedLayoutWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.FillExpectedLayoutWorker_RunWorkerCompleted);
         // 
         // serviceController1
         // 
         this.serviceController1.MachineName = "127.0.0.1";
         this.serviceController1.ServiceName = "LiquesceSvc";
         // 
         // versionNumberToolStripMenuItem
         // 
         this.versionNumberToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
         this.versionNumberToolStripMenuItem.Enabled = false;
         this.versionNumberToolStripMenuItem.Name = "versionNumberToolStripMenuItem";
         this.versionNumberToolStripMenuItem.Size = new System.Drawing.Size(106, 20);
         this.versionNumberToolStripMenuItem.Text = "Version Number";
         this.versionNumberToolStripMenuItem.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
         this.ClientSize = new System.Drawing.Size(770, 489);
         this.Controls.Add(this.splitContainer1);
         this.Controls.Add(this.menuStrip1);
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MainMenuStrip = this.menuStrip1;
         this.MinimumSize = new System.Drawing.Size(778, 516);
         this.Name = "MainForm";
         this.Text = "Liquesce Mount Management";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
         this.Shown += new System.EventHandler(this.MainForm_Shown);
         this.splitContainer1.Panel1.ResumeLayout(false);
         this.splitContainer1.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
         this.splitContainer1.ResumeLayout(false);
         this.splitContainer2.Panel1.ResumeLayout(false);
         this.splitContainer2.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
         this.splitContainer2.ResumeLayout(false);
         this.splitContainer3.Panel1.ResumeLayout(false);
         this.splitContainer3.Panel2.ResumeLayout(false);
         this.splitContainer3.Panel2.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
         this.splitContainer3.ResumeLayout(false);
         this.mergeListContext.ResumeLayout(false);
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.DelayCreation)).EndInit();
         this.refreshExpected.ResumeLayout(false);
         this.menuStrip1.ResumeLayout(false);
         this.menuStrip1.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.SplitContainer splitContainer1;
      private System.Windows.Forms.TreeView driveAndDirTreeView;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.SplitContainer splitContainer2;
      private System.Windows.Forms.TreeView mergeList;
      private System.Windows.Forms.ProgressBar progressBar1;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.MenuStrip menuStrip1;
      private System.Windows.Forms.ToolStripMenuItem commitToolStripMenuItem;
      private System.Windows.Forms.TreeView expectedTreeView;
      private System.Windows.Forms.Label label3;
      private System.ComponentModel.BackgroundWorker FillExpectedLayoutWorker;
      private System.Windows.Forms.Label label4;
      private System.Windows.Forms.NumericUpDown DelayCreation;
      private System.Windows.Forms.ComboBox MountPoint;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.TextBox VolumeLabel;
      private System.Windows.Forms.ToolStripMenuItem advancedToolStripMenuItem;
      private System.Windows.Forms.ImageList imageListUnits;
      private System.ServiceProcess.ServiceController serviceController1;
      private System.Windows.Forms.ContextMenuStrip mergeListContext;
      private System.Windows.Forms.ToolStripMenuItem mergeListContextMenuItem;
      private System.Windows.Forms.ContextMenuStrip refreshExpected;
      private System.Windows.Forms.ToolStripMenuItem refreshExpectedToolStripMenuItem;
      private System.Windows.Forms.SplitContainer splitContainer3;
      private System.Windows.Forms.ToolStripMenuItem globalConfigSettingsToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem logsToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem serviceLogViewToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem userLogViewToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem currentSharesToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem versionNumberToolStripMenuItem;
   }
}