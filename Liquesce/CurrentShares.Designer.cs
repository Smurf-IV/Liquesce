namespace Liquesce
{
   partial class CurrentShares
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
         System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CurrentShares));
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.label1 = new System.Windows.Forms.Label();
         this.Store = new System.Windows.Forms.Button();
         this.groupBox2 = new System.Windows.Forms.GroupBox();
         this.mountedPoints = new System.Windows.Forms.TextBox();
         this.progressBar1 = new System.Windows.Forms.ProgressBar();
         this.dataGridView1 = new System.Windows.Forms.DataGridView();
         this.Source = new System.Windows.Forms.DataGridViewTextBoxColumn();
         this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
         this.Credentials = new System.Windows.Forms.DataGridViewTextBoxColumn();
         this.buttonSave = new System.Windows.Forms.Button();
         this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
         this.groupBox1.SuspendLayout();
         this.groupBox2.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
         this.SuspendLayout();
         // 
         // groupBox1
         // 
         this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBox1.Controls.Add(this.label1);
         this.groupBox1.Location = new System.Drawing.Point(15, 5);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(378, 78);
         this.groupBox1.TabIndex = 0;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Notes :";
         // 
         // label1
         // 
         this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.label1.Location = new System.Drawing.Point(3, 18);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(372, 57);
         this.label1.TabIndex = 0;
         this.label1.Text = "The current active \"Mount Points\" owned by Liquesce will need to be renabled afte" +
             "r the service has been started. Therefore you will need to confirm that they are" +
             " have been found correctly.";
         // 
         // Store
         // 
         this.Store.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.Store.Location = new System.Drawing.Point(237, 366);
         this.Store.Name = "Store";
         this.Store.Size = new System.Drawing.Size(75, 23);
         this.Store.TabIndex = 1;
         this.Store.Text = "&Refresh";
         this.toolTip1.SetToolTip(this.Store, "Setup the shares first and then press refresh");
         this.Store.UseVisualStyleBackColor = true;
         this.Store.Click += new System.EventHandler(this.Store_Click);
         // 
         // groupBox2
         // 
         this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBox2.Controls.Add(this.mountedPoints);
         this.groupBox2.Location = new System.Drawing.Point(15, 89);
         this.groupBox2.Name = "groupBox2";
         this.groupBox2.Size = new System.Drawing.Size(380, 53);
         this.groupBox2.TabIndex = 2;
         this.groupBox2.TabStop = false;
         this.groupBox2.Text = "Mounted Points :";
         // 
         // mountedPoints
         // 
         this.mountedPoints.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.mountedPoints.Location = new System.Drawing.Point(8, 22);
         this.mountedPoints.Name = "mountedPoints";
         this.mountedPoints.ReadOnly = true;
         this.mountedPoints.Size = new System.Drawing.Size(366, 22);
         this.mountedPoints.TabIndex = 0;
         this.toolTip1.SetToolTip(this.mountedPoints, "Current drive matching pattern to search for the shares and creadentials");
         // 
         // progressBar1
         // 
         this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.progressBar1.Location = new System.Drawing.Point(13, 366);
         this.progressBar1.Name = "progressBar1";
         this.progressBar1.Size = new System.Drawing.Size(218, 23);
         this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
         this.progressBar1.TabIndex = 4;
         // 
         // dataGridView1
         // 
         this.dataGridView1.AllowUserToAddRows = false;
         this.dataGridView1.AllowUserToDeleteRows = false;
         this.dataGridView1.AllowUserToOrderColumns = true;
         dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
         this.dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
         this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                     | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
         this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Source,
            this.Description,
            this.Credentials});
         this.dataGridView1.Location = new System.Drawing.Point(13, 148);
         this.dataGridView1.Name = "dataGridView1";
         this.dataGridView1.ReadOnly = true;
         this.dataGridView1.RowHeadersVisible = false;
         this.dataGridView1.ShowEditingIcon = false;
         this.dataGridView1.Size = new System.Drawing.Size(380, 212);
         this.dataGridView1.TabIndex = 5;
         this.toolTip1.SetToolTip(this.dataGridView1, "Any settings seen here will be the one used in the share restore operation");
         // 
         // Source
         // 
         this.Source.Frozen = true;
         this.Source.HeaderText = "Source";
         this.Source.Name = "Source";
         this.Source.ReadOnly = true;
         // 
         // Description
         // 
         this.Description.HeaderText = "Description";
         this.Description.Name = "Description";
         this.Description.ReadOnly = true;
         // 
         // Credentials
         // 
         this.Credentials.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
         this.Credentials.HeaderText = "Credentials";
         this.Credentials.MinimumWidth = 50;
         this.Credentials.Name = "Credentials";
         this.Credentials.ReadOnly = true;
         // 
         // buttonSave
         // 
         this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonSave.Location = new System.Drawing.Point(318, 366);
         this.buttonSave.Name = "buttonSave";
         this.buttonSave.Size = new System.Drawing.Size(75, 23);
         this.buttonSave.TabIndex = 6;
         this.buttonSave.Text = "&Save";
         this.toolTip1.SetToolTip(this.buttonSave, "The Settings above will be stored - ready to be sent to the server");
         this.buttonSave.UseVisualStyleBackColor = true;
         this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
         // 
         // CurrentShares
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(405, 402);
         this.Controls.Add(this.buttonSave);
         this.Controls.Add(this.dataGridView1);
         this.Controls.Add(this.progressBar1);
         this.Controls.Add(this.groupBox2);
         this.Controls.Add(this.Store);
         this.Controls.Add(this.groupBox1);
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(413, 429);
         this.Name = "CurrentShares";
         this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
         this.Text = "Current Shares";
         this.Shown += new System.EventHandler(this.CurrentShares_Shown);
         this.groupBox1.ResumeLayout(false);
         this.groupBox2.ResumeLayout(false);
         this.groupBox2.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.Button Store;
      private System.Windows.Forms.GroupBox groupBox2;
      private System.Windows.Forms.TextBox mountedPoints;
      private System.Windows.Forms.ProgressBar progressBar1;
      private System.Windows.Forms.DataGridView dataGridView1;
      private System.Windows.Forms.DataGridViewTextBoxColumn Source;
      private System.Windows.Forms.DataGridViewTextBoxColumn Description;
      private System.Windows.Forms.DataGridViewTextBoxColumn Credentials;
      private System.Windows.Forms.Button buttonSave;
      private System.Windows.Forms.ToolTip toolTip1;
   }
}