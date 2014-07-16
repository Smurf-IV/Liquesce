namespace Liquesce
{
   partial class TabMainForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TabMainForm));
         this.tabControl1 = new System.Windows.Forms.TabControl();
         this.tabWelcome = new System.Windows.Forms.TabPage();
         this.welcome1 = new Liquesce.Tabs.Welcome();
         this.tabLogging = new System.Windows.Forms.TabPage();
         this.logging1 = new Liquesce.Tabs.Logging();
         this.tabMounting = new System.Windows.Forms.TabPage();
         this.mountingPoints1 = new Liquesce.Tabs.MountingPoints();
         this.tabSharing = new System.Windows.Forms.TabPage();
         this.currentShares1 = new Liquesce.Tabs.CurrentShares();
         this.tabService = new System.Windows.Forms.TabPage();
         this.service1 = new Liquesce.Tabs.Service();
         this.tabControl1.SuspendLayout();
         this.tabWelcome.SuspendLayout();
         this.tabLogging.SuspendLayout();
         this.tabMounting.SuspendLayout();
         this.tabSharing.SuspendLayout();
         this.tabService.SuspendLayout();
         this.SuspendLayout();
         // 
         // tabControl1
         // 
         this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Left;
         this.tabControl1.Controls.Add(this.tabWelcome);
         this.tabControl1.Controls.Add(this.tabLogging);
         this.tabControl1.Controls.Add(this.tabMounting);
         this.tabControl1.Controls.Add(this.tabSharing);
         this.tabControl1.Controls.Add(this.tabService);
         this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
         this.tabControl1.ItemSize = new System.Drawing.Size(25, 150);
         this.tabControl1.Location = new System.Drawing.Point(3, 3);
         this.tabControl1.Multiline = true;
         this.tabControl1.Name = "tabControl1";
         this.tabControl1.SelectedIndex = 0;
         this.tabControl1.Size = new System.Drawing.Size(764, 483);
         this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
         this.tabControl1.TabIndex = 0;
         this.tabControl1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.tabControl1_DrawItem);
         // 
         // tabWelcome
         // 
         this.tabWelcome.BackColor = System.Drawing.SystemColors.Control;
         this.tabWelcome.Controls.Add(this.welcome1);
         this.tabWelcome.Location = new System.Drawing.Point(154, 4);
         this.tabWelcome.Name = "tabWelcome";
         this.tabWelcome.Padding = new System.Windows.Forms.Padding(3);
         this.tabWelcome.Size = new System.Drawing.Size(606, 475);
         this.tabWelcome.TabIndex = 0;
         this.tabWelcome.Text = "Welcome";
         // 
         // welcome1
         // 
         this.welcome1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.welcome1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.welcome1.Location = new System.Drawing.Point(3, 3);
         this.welcome1.Name = "welcome1";
         this.welcome1.Padding = new System.Windows.Forms.Padding(3);
         this.welcome1.Size = new System.Drawing.Size(600, 469);
         this.welcome1.TabIndex = 0;
         // 
         // tabLogging
         // 
         this.tabLogging.BackColor = System.Drawing.SystemColors.Control;
         this.tabLogging.Controls.Add(this.logging1);
         this.tabLogging.Location = new System.Drawing.Point(154, 4);
         this.tabLogging.Name = "tabLogging";
         this.tabLogging.Padding = new System.Windows.Forms.Padding(3);
         this.tabLogging.Size = new System.Drawing.Size(606, 475);
         this.tabLogging.TabIndex = 1;
         this.tabLogging.Text = "Logging";
         // 
         // logging1
         // 
         this.logging1.BackColor = System.Drawing.SystemColors.Control;
         this.logging1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.logging1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.logging1.Location = new System.Drawing.Point(3, 3);
         this.logging1.Name = "logging1";
         this.logging1.Padding = new System.Windows.Forms.Padding(3);
         this.logging1.Size = new System.Drawing.Size(600, 469);
         this.logging1.TabIndex = 0;
         // 
         // tabMounting
         // 
         this.tabMounting.BackColor = System.Drawing.SystemColors.Control;
         this.tabMounting.Controls.Add(this.mountingPoints1);
         this.tabMounting.Location = new System.Drawing.Point(154, 4);
         this.tabMounting.Name = "tabMounting";
         this.tabMounting.Size = new System.Drawing.Size(606, 475);
         this.tabMounting.TabIndex = 2;
         this.tabMounting.Text = "Mounting Point";
         // 
         // mountingPoints1
         // 
         this.mountingPoints1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.mountingPoints1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.mountingPoints1.Location = new System.Drawing.Point(0, 0);
         this.mountingPoints1.Name = "mountingPoints1";
         this.mountingPoints1.Padding = new System.Windows.Forms.Padding(3);
         this.mountingPoints1.Size = new System.Drawing.Size(606, 475);
         this.mountingPoints1.TabIndex = 0;
         // 
         // tabSharing
         // 
         this.tabSharing.BackColor = System.Drawing.SystemColors.Control;
         this.tabSharing.Controls.Add(this.currentShares1);
         this.tabSharing.Location = new System.Drawing.Point(154, 4);
         this.tabSharing.Name = "tabSharing";
         this.tabSharing.Padding = new System.Windows.Forms.Padding(3);
         this.tabSharing.Size = new System.Drawing.Size(606, 475);
         this.tabSharing.TabIndex = 4;
         this.tabSharing.Text = "Sharing Control";
         // 
         // currentShares1
         // 
         this.currentShares1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.currentShares1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.currentShares1.Location = new System.Drawing.Point(3, 3);
         this.currentShares1.Name = "currentShares1";
         this.currentShares1.Size = new System.Drawing.Size(600, 469);
         this.currentShares1.TabIndex = 0;
         // 
         // tabService
         // 
         this.tabService.BackColor = System.Drawing.SystemColors.Control;
         this.tabService.Controls.Add(this.service1);
         this.tabService.Location = new System.Drawing.Point(154, 4);
         this.tabService.Name = "tabService";
         this.tabService.Size = new System.Drawing.Size(606, 475);
         this.tabService.TabIndex = 3;
         this.tabService.Text = "Service Settings";
         // 
         // service1
         // 
         this.service1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.service1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.service1.Location = new System.Drawing.Point(0, 0);
         this.service1.Name = "service1";
         this.service1.Padding = new System.Windows.Forms.Padding(3);
         this.service1.Size = new System.Drawing.Size(606, 475);
         this.service1.TabIndex = 0;
         // 
         // TabMainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(770, 489);
         this.Controls.Add(this.tabControl1);
         this.DoubleBuffered = true;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MinimumSize = new System.Drawing.Size(778, 516);
         this.Name = "TabMainForm";
         this.Padding = new System.Windows.Forms.Padding(3);
         this.Text = "Liquesce ][ Mount Manager";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LandingZone_FormClosing);
         this.Shown += new System.EventHandler(this.TabMainForm_Shown);
         this.tabControl1.ResumeLayout(false);
         this.tabWelcome.ResumeLayout(false);
         this.tabLogging.ResumeLayout(false);
         this.tabMounting.ResumeLayout(false);
         this.tabSharing.ResumeLayout(false);
         this.tabService.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.TabControl tabControl1;
      private System.Windows.Forms.TabPage tabWelcome;
      private System.Windows.Forms.TabPage tabLogging;
      private Tabs.Welcome welcome1;
      private Tabs.Logging logging1;
      private System.Windows.Forms.TabPage tabMounting;
      private Tabs.MountingPoints mountingPoints1;
      private System.Windows.Forms.TabPage tabService;
      private Tabs.Service service1;
      private System.Windows.Forms.TabPage tabSharing;
      private Tabs.CurrentShares currentShares1;
   }
}