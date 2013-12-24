namespace Liquesce.Mounting
{
   partial class Edit
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
         this.imageListUnits = new System.Windows.Forms.ImageList(this.components);
         this.mergeListContext = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.refreshExpected = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.refreshExpectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.mergeListContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
         this.driveAndDirTreeView = new System.Windows.Forms.TreeView();
         this.mergeList = new System.Windows.Forms.DataGridView();
         this.Source = new System.Windows.Forms.DataGridViewTextBoxColumn();
         this.IncludeName = new System.Windows.Forms.DataGridViewCheckBoxColumn();
         this.AsReadOnly = new System.Windows.Forms.DataGridViewCheckBoxColumn();
         this.VolumeLabel = new System.Windows.Forms.TextBox();
         this.groupBox2 = new System.Windows.Forms.GroupBox();
         this.txtFolder = new System.Windows.Forms.TextBox();
         this.lblFolder = new System.Windows.Forms.Label();
         this.label5 = new System.Windows.Forms.Label();
         this.MountPoint = new System.Windows.Forms.ComboBox();
         this.expectedTreeView = new System.Windows.Forms.TreeView();
         this.label3 = new System.Windows.Forms.Label();
         this.FillExpectedLayoutWorker = new System.ComponentModel.BackgroundWorker();
         this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
         this.splitContainer1 = new Liquesce.SplitContainerEx();
         this.label1 = new System.Windows.Forms.Label();
         this.splitContainer2 = new Liquesce.SplitContainerEx();
         this.splitContainer3 = new Liquesce.SplitContainerEx();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.label2 = new System.Windows.Forms.Label();
         this.progressBar1 = new System.Windows.Forms.ProgressBar();
         this.mergeListContext.SuspendLayout();
         this.refreshExpected.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.mergeList)).BeginInit();
         this.groupBox2.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
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
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // imageListUnits
         // 
         this.imageListUnits.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
         this.imageListUnits.ImageSize = new System.Drawing.Size(16, 16);
         this.imageListUnits.TransparentColor = System.Drawing.Color.Transparent;
         // 
         // mergeListContext
         // 
         this.mergeListContext.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.mergeListContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem});
         this.mergeListContext.Name = "mergeListContext";
         this.mergeListContext.Size = new System.Drawing.Size(135, 26);
         this.mergeListContext.Click += new System.EventHandler(this.mergeListContextMenuItem_Click);
         // 
         // deleteToolStripMenuItem
         // 
         this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
         this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
         this.deleteToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
         this.deleteToolStripMenuItem.Text = "&Delete";
         this.deleteToolStripMenuItem.ToolTipText = "Delete the current selected item.";
         // 
         // refreshExpected
         // 
         this.refreshExpected.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.refreshExpected.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshExpectedToolStripMenuItem});
         this.refreshExpected.Name = "refreshExpected";
         this.refreshExpected.Size = new System.Drawing.Size(192, 26);
         // 
         // refreshExpectedToolStripMenuItem
         // 
         this.refreshExpectedToolStripMenuItem.Name = "refreshExpectedToolStripMenuItem";
         this.refreshExpectedToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
         this.refreshExpectedToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
         this.refreshExpectedToolStripMenuItem.Text = "&Refresh Expected";
         this.refreshExpectedToolStripMenuItem.Click += new System.EventHandler(this.refreshExpectedToolStripMenuItem_Click);
         // 
         // mergeListContextMenuItem
         // 
         this.mergeListContextMenuItem.Name = "mergeListContextMenuItem";
         this.mergeListContextMenuItem.Size = new System.Drawing.Size(32, 19);
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
         this.driveAndDirTreeView.Size = new System.Drawing.Size(198, 458);
         this.driveAndDirTreeView.TabIndex = 1;
         this.toolTip1.SetToolTip(this.driveAndDirTreeView, "Drag from here and drop in the middle");
         this.driveAndDirTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.driveAndDirTreeView_BeforeExpand);
         this.driveAndDirTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.driveAndDirTreeView_MouseDown);
         // 
         // mergeList
         // 
         this.mergeList.AllowDrop = true;
         this.mergeList.AllowUserToAddRows = false;
         this.mergeList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
         this.mergeList.BackgroundColor = System.Drawing.SystemColors.Window;
         this.mergeList.BorderStyle = System.Windows.Forms.BorderStyle.None;
         this.mergeList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
         this.mergeList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Source,
            this.IncludeName,
            this.AsReadOnly});
         this.mergeList.ContextMenuStrip = this.mergeListContext;
         this.mergeList.Dock = System.Windows.Forms.DockStyle.Fill;
         this.mergeList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
         this.mergeList.Location = new System.Drawing.Point(0, 0);
         this.mergeList.MultiSelect = false;
         this.mergeList.Name = "mergeList";
         this.mergeList.RowHeadersVisible = false;
         this.mergeList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
         this.mergeList.Size = new System.Drawing.Size(186, 325);
         this.mergeList.TabIndex = 1;
         this.toolTip1.SetToolTip(this.mergeList, "Use mouse to change order and drop new items.");
         this.mergeList.DragDrop += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragDrop);
         this.mergeList.DragOver += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragOver);
         this.mergeList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.mergeList_KeyUp);
         this.mergeList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseDown);
         // 
         // Source
         // 
         this.Source.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
         this.Source.FillWeight = 60F;
         this.Source.HeaderText = "Source";
         this.Source.Name = "Source";
         this.Source.ReadOnly = true;
         this.Source.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
         // 
         // IncludeName
         // 
         this.IncludeName.FillWeight = 20F;
         this.IncludeName.HeaderText = "Inclusive";
         this.IncludeName.Name = "IncludeName";
         this.IncludeName.ToolTipText = "Use the source name as the root folder.";
         // 
         // AsReadOnly
         // 
         this.AsReadOnly.FillWeight = 20F;
         this.AsReadOnly.HeaderText = "Read Only";
         this.AsReadOnly.Name = "AsReadOnly";
         this.AsReadOnly.ToolTipText = "The files will return the read-only attribute and will not be allowed to change.";
         // 
         // VolumeLabel
         // 
         this.VolumeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.VolumeLabel.Location = new System.Drawing.Point(7, 18);
         this.VolumeLabel.MaxLength = 32;
         this.VolumeLabel.Name = "VolumeLabel";
         this.VolumeLabel.Size = new System.Drawing.Size(173, 22);
         this.VolumeLabel.TabIndex = 0;
         this.toolTip1.SetToolTip(this.VolumeLabel, "Label that will be visible in Windows explorer");
         this.VolumeLabel.Validated += new System.EventHandler(this.VolumeLabel_Validated);
         // 
         // groupBox2
         // 
         this.groupBox2.Controls.Add(this.txtFolder);
         this.groupBox2.Controls.Add(this.lblFolder);
         this.groupBox2.Controls.Add(this.label5);
         this.groupBox2.Controls.Add(this.MountPoint);
         this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBox2.Location = new System.Drawing.Point(0, 0);
         this.groupBox2.Name = "groupBox2";
         this.groupBox2.Size = new System.Drawing.Size(186, 79);
         this.groupBox2.TabIndex = 0;
         this.groupBox2.TabStop = false;
         this.groupBox2.Text = "Drive Mounting:";
         this.toolTip1.SetToolTip(this.groupBox2, "Either via Letter or Folder");
         // 
         // txtFolder
         // 
         this.txtFolder.AllowDrop = true;
         this.txtFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.txtFolder.Location = new System.Drawing.Point(56, 49);
         this.txtFolder.Name = "txtFolder";
         this.txtFolder.Size = new System.Drawing.Size(123, 22);
         this.txtFolder.TabIndex = 3;
         this.toolTip1.SetToolTip(this.txtFolder, "NTFS folder mount point.\r\nWill override the above \"Drive Letter\".");
         this.txtFolder.TextChanged += new System.EventHandler(this.txtFolder_TextChanged);
         this.txtFolder.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtFolder_DragDrop);
         this.txtFolder.DragOver += new System.Windows.Forms.DragEventHandler(this.txtFolder_DragOver);
         // 
         // lblFolder
         // 
         this.lblFolder.AutoSize = true;
         this.lblFolder.Location = new System.Drawing.Point(7, 52);
         this.lblFolder.Name = "lblFolder";
         this.lblFolder.Size = new System.Drawing.Size(44, 14);
         this.lblFolder.TabIndex = 2;
         this.lblFolder.Text = "&Folder:";
         this.toolTip1.SetToolTip(this.lblFolder, "NTFS folder mount point.\r\nWill override the above \"Drive Letter\".");
         // 
         // label5
         // 
         this.label5.AutoSize = true;
         this.label5.Location = new System.Drawing.Point(6, 24);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(45, 14);
         this.label5.TabIndex = 0;
         this.label5.Text = "&Letter:";
         this.toolTip1.SetToolTip(this.label5, "Drive letter to be used for the new volume");
         // 
         // MountPoint
         // 
         this.MountPoint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.MountPoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.MountPoint.FormattingEnabled = true;
         this.MountPoint.Items.AddRange(new object[] {
            "A",
            "B",
            "C",
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
         this.MountPoint.Location = new System.Drawing.Point(57, 21);
         this.MountPoint.Name = "MountPoint";
         this.MountPoint.Size = new System.Drawing.Size(123, 22);
         this.MountPoint.Sorted = true;
         this.MountPoint.TabIndex = 1;
         this.toolTip1.SetToolTip(this.MountPoint, "Drive letter to be used for the new volume");
         this.MountPoint.TextChanged += new System.EventHandler(this.MountPoint_TextChanged);
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
         this.expectedTreeView.Size = new System.Drawing.Size(208, 435);
         this.expectedTreeView.TabIndex = 1;
         this.toolTip1.SetToolTip(this.expectedTreeView, "Expand to see if any duplicates have been found");
         this.expectedTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.expectedTreeView_BeforeExpand);
         // 
         // label3
         // 
         this.label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label3.Dock = System.Windows.Forms.DockStyle.Top;
         this.label3.Location = new System.Drawing.Point(0, 0);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(208, 17);
         this.label3.TabIndex = 0;
         this.label3.Text = "&Merged layout :-";
         this.toolTip1.SetToolTip(this.label3, "Look for duplicates to avoid collisions later on");
         // 
         // FillExpectedLayoutWorker
         // 
         this.FillExpectedLayoutWorker.WorkerReportsProgress = true;
         this.FillExpectedLayoutWorker.WorkerSupportsCancellation = true;
         this.FillExpectedLayoutWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.FillExpectedLayoutWorker_DoWork);
         this.FillExpectedLayoutWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.FillExpectedLayoutWorker_RunWorkerCompleted);
         // 
         // errorProvider1
         // 
         this.errorProvider1.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.AlwaysBlink;
         this.errorProvider1.ContainerControl = this;
         // 
         // splitContainer1
         // 
         this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer1.Location = new System.Drawing.Point(0, 0);
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
         this.splitContainer1.Size = new System.Drawing.Size(600, 475);
         this.splitContainer1.SplitterDistance = 198;
         this.splitContainer1.SplitterWidth = 4;
         this.splitContainer1.TabIndex = 0;
         // 
         // label1
         // 
         this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label1.Dock = System.Windows.Forms.DockStyle.Top;
         this.label1.Location = new System.Drawing.Point(0, 0);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(198, 17);
         this.label1.TabIndex = 0;
         this.label1.Text = "&Host file system:-";
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
         this.splitContainer2.Size = new System.Drawing.Size(398, 475);
         this.splitContainer2.SplitterDistance = 186;
         this.splitContainer2.SplitterWidth = 4;
         this.splitContainer2.TabIndex = 0;
         // 
         // splitContainer3
         // 
         this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
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
         this.splitContainer3.Panel2.Controls.Add(this.groupBox2);
         this.splitContainer3.Size = new System.Drawing.Size(186, 458);
         this.splitContainer3.SplitterDistance = 325;
         this.splitContainer3.SplitterWidth = 4;
         this.splitContainer3.TabIndex = 7;
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.VolumeLabel);
         this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBox1.Location = new System.Drawing.Point(0, 79);
         this.groupBox1.Margin = new System.Windows.Forms.Padding(0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(186, 44);
         this.groupBox1.TabIndex = 1;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "&Volume Label :";
         // 
         // label2
         // 
         this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label2.Dock = System.Windows.Forms.DockStyle.Top;
         this.label2.Location = new System.Drawing.Point(0, 0);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(186, 17);
         this.label2.TabIndex = 0;
         this.label2.Text = "&Source folders:-";
         // 
         // progressBar1
         // 
         this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.progressBar1.ForeColor = System.Drawing.Color.LawnGreen;
         this.progressBar1.Location = new System.Drawing.Point(0, 452);
         this.progressBar1.Name = "progressBar1";
         this.progressBar1.Size = new System.Drawing.Size(208, 23);
         this.progressBar1.Step = 5;
         this.progressBar1.TabIndex = 4;
         // 
         // Edit
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.splitContainer1);
         this.DoubleBuffered = true;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "Edit";
         this.Size = new System.Drawing.Size(600, 475);
         this.Load += new System.EventHandler(this.MountingPoints_Load);
         this.Leave += new System.EventHandler(this.MountingPoints_Leave);
         this.mergeListContext.ResumeLayout(false);
         this.refreshExpected.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.mergeList)).EndInit();
         this.groupBox2.ResumeLayout(false);
         this.groupBox2.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
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
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
         this.splitContainer3.ResumeLayout(false);
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private SplitContainerEx splitContainer1;
      private System.Windows.Forms.TreeView driveAndDirTreeView;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Label label1;
      private SplitContainerEx splitContainer2;
      private System.Windows.Forms.ProgressBar progressBar1;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.TreeView expectedTreeView;
      private System.Windows.Forms.Label label3;
      private System.ComponentModel.BackgroundWorker FillExpectedLayoutWorker;
      private System.Windows.Forms.ComboBox MountPoint;
      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.TextBox VolumeLabel;
      private System.Windows.Forms.ImageList imageListUnits;
      private System.Windows.Forms.ContextMenuStrip mergeListContext;
      private System.Windows.Forms.ToolStripMenuItem mergeListContextMenuItem;
      private System.Windows.Forms.ContextMenuStrip refreshExpected;
      private System.Windows.Forms.ToolStripMenuItem refreshExpectedToolStripMenuItem;
      private SplitContainerEx splitContainer3;
      private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
      private System.Windows.Forms.GroupBox groupBox2;
      private System.Windows.Forms.TextBox txtFolder;
      private System.Windows.Forms.Label lblFolder;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.ErrorProvider errorProvider1;
      private System.Windows.Forms.DataGridView mergeList;
      private System.Windows.Forms.DataGridViewTextBoxColumn Source;
      private System.Windows.Forms.DataGridViewCheckBoxColumn IncludeName;
      private System.Windows.Forms.DataGridViewCheckBoxColumn AsReadOnly;
   }
}