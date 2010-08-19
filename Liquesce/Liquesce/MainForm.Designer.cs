﻿namespace Liquesce
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
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.splitContainer2 = new System.Windows.Forms.SplitContainer();
         this.treeView1 = new System.Windows.Forms.TreeView();
         this.treeView2 = new System.Windows.Forms.TreeView();
         this.label1 = new System.Windows.Forms.Label();
         this.label2 = new System.Windows.Forms.Label();
         this.label3 = new System.Windows.Forms.Label();
         this.listBox1 = new System.Windows.Forms.ListBox();
         this.toolTip1 = new System.Windows.Forms.ToolTip( this.components );
         this.progressBar1 = new System.Windows.Forms.ProgressBar();
         this.FillExpectedLayoutWorker = new System.ComponentModel.BackgroundWorker();
         this.menuStrip1 = new System.Windows.Forms.MenuStrip();
         this.commitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.panel1 = new System.Windows.Forms.Panel();
         this.DelayCreation = new System.Windows.Forms.NumericUpDown();
         this.label4 = new System.Windows.Forms.Label();
         this.label5 = new System.Windows.Forms.Label();
         this.MountPoint = new System.Windows.Forms.ComboBox();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.VolumeLabel = new System.Windows.Forms.TextBox();
         this.advancedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
         this.splitContainer2.Panel1.SuspendLayout();
         this.splitContainer2.Panel2.SuspendLayout();
         this.splitContainer2.SuspendLayout();
         this.menuStrip1.SuspendLayout();
         this.panel1.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.DelayCreation)).BeginInit();
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer1
         // 
         this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer1.Location = new System.Drawing.Point( 0, 0 );
         this.splitContainer1.Name = "splitContainer1";
         // 
         // splitContainer1.Panel1
         // 
         this.splitContainer1.Panel1.Controls.Add( this.treeView1 );
         this.splitContainer1.Panel1.Controls.Add( this.label1 );
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.Controls.Add( this.splitContainer2 );
         this.splitContainer1.Size = new System.Drawing.Size( 770, 489 );
         this.splitContainer1.SplitterDistance = 256;
         this.splitContainer1.SplitterWidth = 5;
         this.splitContainer1.TabIndex = 0;
         // 
         // splitContainer2
         // 
         this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer2.Location = new System.Drawing.Point( 0, 0 );
         this.splitContainer2.Name = "splitContainer2";
         // 
         // splitContainer2.Panel1
         // 
         this.splitContainer2.Panel1.Controls.Add( this.listBox1 );
         this.splitContainer2.Panel1.Controls.Add( this.panel1 );
         this.splitContainer2.Panel1.Controls.Add( this.progressBar1 );
         this.splitContainer2.Panel1.Controls.Add( this.label2 );
         this.splitContainer2.Panel1.Controls.Add( this.menuStrip1 );
         // 
         // splitContainer2.Panel2
         // 
         this.splitContainer2.Panel2.Controls.Add( this.treeView2 );
         this.splitContainer2.Panel2.Controls.Add( this.label3 );
         this.splitContainer2.Size = new System.Drawing.Size( 509, 489 );
         this.splitContainer2.SplitterDistance = 247;
         this.splitContainer2.SplitterWidth = 5;
         this.splitContainer2.TabIndex = 0;
         // 
         // treeView1
         // 
         this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.treeView1.Location = new System.Drawing.Point( 0, 17 );
         this.treeView1.Name = "treeView1";
         this.treeView1.Size = new System.Drawing.Size( 256, 472 );
         this.treeView1.TabIndex = 0;
         this.toolTip1.SetToolTip( this.treeView1, "Drag from here and drop in the middle" );
         // 
         // treeView2
         // 
         this.treeView2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.treeView2.Location = new System.Drawing.Point( 0, 17 );
         this.treeView2.Name = "treeView2";
         this.treeView2.Size = new System.Drawing.Size( 257, 472 );
         this.treeView2.TabIndex = 0;
         this.toolTip1.SetToolTip( this.treeView2, "This will refresh slowly as the dir list is built up" );
         // 
         // label1
         // 
         this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label1.Dock = System.Windows.Forms.DockStyle.Top;
         this.label1.Location = new System.Drawing.Point( 0, 0 );
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size( 256, 17 );
         this.label1.TabIndex = 1;
         this.label1.Text = "This file system:-";
         // 
         // label2
         // 
         this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label2.Dock = System.Windows.Forms.DockStyle.Top;
         this.label2.Location = new System.Drawing.Point( 0, 0 );
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size( 247, 17 );
         this.label2.TabIndex = 2;
         this.label2.Text = "Merge points:-";
         // 
         // label3
         // 
         this.label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
         this.label3.Dock = System.Windows.Forms.DockStyle.Top;
         this.label3.Location = new System.Drawing.Point( 0, 0 );
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size( 257, 17 );
         this.label3.TabIndex = 2;
         this.label3.Text = "Expected layout :-";
         // 
         // listBox1
         // 
         this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.listBox1.FormattingEnabled = true;
         this.listBox1.ItemHeight = 14;
         this.listBox1.Location = new System.Drawing.Point( 0, 17 );
         this.listBox1.Name = "listBox1";
         this.listBox1.Size = new System.Drawing.Size( 247, 324 );
         this.listBox1.TabIndex = 3;
         this.toolTip1.SetToolTip( this.listBox1, "Drop the Entries here" );
         // 
         // progressBar1
         // 
         this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.progressBar1.Location = new System.Drawing.Point( 0, 442 );
         this.progressBar1.Name = "progressBar1";
         this.progressBar1.Size = new System.Drawing.Size( 247, 23 );
         this.progressBar1.TabIndex = 4;
         // 
         // FillExpectedLayoutWorker
         // 
         this.FillExpectedLayoutWorker.WorkerReportsProgress = true;
         this.FillExpectedLayoutWorker.WorkerSupportsCancellation = true;
         // 
         // menuStrip1
         // 
         this.menuStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.menuStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.commitToolStripMenuItem,
            this.advancedToolStripMenuItem} );
         this.menuStrip1.Location = new System.Drawing.Point( 0, 465 );
         this.menuStrip1.Name = "menuStrip1";
         this.menuStrip1.Size = new System.Drawing.Size( 247, 24 );
         this.menuStrip1.TabIndex = 5;
         this.menuStrip1.Text = "menuStrip1";
         // 
         // commitToolStripMenuItem
         // 
         this.commitToolStripMenuItem.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
         this.commitToolStripMenuItem.Name = "commitToolStripMenuItem";
         this.commitToolStripMenuItem.Size = new System.Drawing.Size( 60, 20 );
         this.commitToolStripMenuItem.Text = "&Commit";
         this.commitToolStripMenuItem.ToolTipText = "Send the information above to the service";
         // 
         // panel1
         // 
         this.panel1.Controls.Add( this.groupBox1 );
         this.panel1.Controls.Add( this.MountPoint );
         this.panel1.Controls.Add( this.label5 );
         this.panel1.Controls.Add( this.label4 );
         this.panel1.Controls.Add( this.DelayCreation );
         this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.panel1.Location = new System.Drawing.Point( 0, 341 );
         this.panel1.Name = "panel1";
         this.panel1.Size = new System.Drawing.Size( 247, 101 );
         this.panel1.TabIndex = 6;
         // 
         // DelayCreation
         // 
         this.DelayCreation.Increment = new decimal( new int[] {
            100,
            0,
            0,
            0} );
         this.DelayCreation.Location = new System.Drawing.Point( 143, 4 );
         this.DelayCreation.Maximum = new decimal( new int[] {
            300000,
            0,
            0,
            0} );
         this.DelayCreation.Minimum = new decimal( new int[] {
            5000,
            0,
            0,
            0} );
         this.DelayCreation.Name = "DelayCreation";
         this.DelayCreation.Size = new System.Drawing.Size( 101, 22 );
         this.DelayCreation.TabIndex = 0;
         this.DelayCreation.ThousandsSeparator = true;
         this.toolTip1.SetToolTip( this.DelayCreation, "The amount of time to give the service to allow the drives to attach in windows" );
         this.DelayCreation.Value = new decimal( new int[] {
            15000,
            0,
            0,
            0} );
         // 
         // label4
         // 
         this.label4.AutoSize = true;
         this.label4.Location = new System.Drawing.Point( 4, 6 );
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size( 118, 14 );
         this.label4.TabIndex = 1;
         this.label4.Text = "&Delay Creation (ms):";
         // 
         // label5
         // 
         this.label5.AutoSize = true;
         this.label5.Location = new System.Drawing.Point( 7, 35 );
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size( 77, 14 );
         this.label5.TabIndex = 2;
         this.label5.Text = "Drive &Mount:";
         // 
         // MountPoint
         // 
         this.MountPoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.MountPoint.FormattingEnabled = true;
         this.MountPoint.Items.AddRange( new object[] {
            "E",
            "F",
            "G",
            "H",
            "N"} );
         this.MountPoint.Location = new System.Drawing.Point( 90, 32 );
         this.MountPoint.Name = "MountPoint";
         this.MountPoint.Size = new System.Drawing.Size( 154, 22 );
         this.MountPoint.TabIndex = 3;
         this.toolTip1.SetToolTip( this.MountPoint, "Drive letter to be used for the new volume" );
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add( this.VolumeLabel );
         this.groupBox1.Location = new System.Drawing.Point( 4, 53 );
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size( 240, 43 );
         this.groupBox1.TabIndex = 4;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "&Volume Label :";
         // 
         // VolumeLabel
         // 
         this.VolumeLabel.Location = new System.Drawing.Point( 7, 17 );
         this.VolumeLabel.MaxLength = 32;
         this.VolumeLabel.Name = "VolumeLabel";
         this.VolumeLabel.Size = new System.Drawing.Size( 227, 22 );
         this.VolumeLabel.TabIndex = 0;
         this.toolTip1.SetToolTip( this.VolumeLabel, "Label that will be visible in Windows explorer" );
         // 
         // advancedToolStripMenuItem
         // 
         this.advancedToolStripMenuItem.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
         this.advancedToolStripMenuItem.Name = "advancedToolStripMenuItem";
         this.advancedToolStripMenuItem.Size = new System.Drawing.Size( 73, 20 );
         this.advancedToolStripMenuItem.Text = "&Advanced";
         this.advancedToolStripMenuItem.ToolTipText = "Advanced options to aid in debug and testing";
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF( 7F, 14F );
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size( 770, 489 );
         this.Controls.Add( this.splitContainer1 );
         this.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
         this.MainMenuStrip = this.menuStrip1;
         this.MinimumSize = new System.Drawing.Size( 778, 516 );
         this.Name = "MainForm";
         this.Text = "Liquesce Mount Management";
         this.Shown += new System.EventHandler( this.MainForm_Shown );
         this.splitContainer1.Panel1.ResumeLayout( false );
         this.splitContainer1.Panel2.ResumeLayout( false );
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
         this.splitContainer1.ResumeLayout( false );
         this.splitContainer2.Panel1.ResumeLayout( false );
         this.splitContainer2.Panel1.PerformLayout();
         this.splitContainer2.Panel2.ResumeLayout( false );
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
         this.splitContainer2.ResumeLayout( false );
         this.menuStrip1.ResumeLayout( false );
         this.menuStrip1.PerformLayout();
         this.panel1.ResumeLayout( false );
         this.panel1.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.DelayCreation)).EndInit();
         this.groupBox1.ResumeLayout( false );
         this.groupBox1.PerformLayout();
         this.ResumeLayout( false );

      }

      #endregion

      private System.Windows.Forms.SplitContainer splitContainer1;
      private System.Windows.Forms.TreeView treeView1;
      private System.Windows.Forms.ToolTip toolTip1;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.SplitContainer splitContainer2;
      private System.Windows.Forms.ListBox listBox1;
      private System.Windows.Forms.ProgressBar progressBar1;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.MenuStrip menuStrip1;
      private System.Windows.Forms.ToolStripMenuItem commitToolStripMenuItem;
      private System.Windows.Forms.TreeView treeView2;
      private System.Windows.Forms.Label label3;
      private System.ComponentModel.BackgroundWorker FillExpectedLayoutWorker;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.Label label4;
      private System.Windows.Forms.NumericUpDown DelayCreation;
      private System.Windows.Forms.ComboBox MountPoint;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.TextBox VolumeLabel;
      private System.Windows.Forms.ToolStripMenuItem advancedToolStripMenuItem;
   }
}