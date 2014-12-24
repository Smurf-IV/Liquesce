namespace Liquesce.Tabs
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

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
         this.dataGridView1 = new System.Windows.Forms.DataGridView();
         this.Source = new System.Windows.Forms.DataGridViewTextBoxColumn();
         this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
         this.Credentials = new System.Windows.Forms.DataGridViewTextBoxColumn();
         this.progressBar1 = new System.Windows.Forms.ProgressBar();
         this.btnRefresh = new System.Windows.Forms.Button();
         this.richTextBox1 = new System.Windows.Forms.RichTextBox();
         ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
         this.SuspendLayout();
         // 
         // dataGridView1
         // 
         this.dataGridView1.AllowUserToAddRows = false;
         this.dataGridView1.AllowUserToDeleteRows = false;
         dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.ControlLight;
         this.dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle2;
         this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
         this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Source,
            this.Description,
            this.Credentials});
         this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
         this.dataGridView1.Location = new System.Drawing.Point(1, 146);
         this.dataGridView1.MultiSelect = false;
         this.dataGridView1.Name = "dataGridView1";
         this.dataGridView1.ReadOnly = true;
         this.dataGridView1.RowHeadersVisible = false;
         this.dataGridView1.ShowEditingIcon = false;
         this.dataGridView1.Size = new System.Drawing.Size(593, 297);
         this.dataGridView1.TabIndex = 10;
         // 
         // Source
         // 
         this.Source.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
         this.Source.HeaderText = "Source Path";
         this.Source.MinimumWidth = 100;
         this.Source.Name = "Source";
         this.Source.ReadOnly = true;
         // 
         // Description
         // 
         this.Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
         this.Description.HeaderText = "Name : Description";
         this.Description.MinimumWidth = 150;
         this.Description.Name = "Description";
         this.Description.ReadOnly = true;
         this.Description.Width = 150;
         // 
         // Credentials
         // 
         this.Credentials.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
         this.Credentials.HeaderText = "User : Credentials";
         this.Credentials.MinimumWidth = 150;
         this.Credentials.Name = "Credentials";
         this.Credentials.ReadOnly = true;
         this.Credentials.Width = 150;
         // 
         // progressBar1
         // 
         this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.progressBar1.Location = new System.Drawing.Point(3, 449);
         this.progressBar1.Name = "progressBar1";
         this.progressBar1.Size = new System.Drawing.Size(512, 23);
         this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
         this.progressBar1.TabIndex = 9;
         // 
         // btnRefresh
         // 
         this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.btnRefresh.Location = new System.Drawing.Point(521, 449);
         this.btnRefresh.Name = "btnRefresh";
         this.btnRefresh.Size = new System.Drawing.Size(75, 23);
         this.btnRefresh.TabIndex = 7;
         this.btnRefresh.Text = "&Refresh";
         this.btnRefresh.UseVisualStyleBackColor = true;
         this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
         // 
         // richTextBox1
         // 
         this.richTextBox1.BackColor = System.Drawing.SystemColors.Control;
         this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Top;
         this.richTextBox1.Location = new System.Drawing.Point(0, 0);
         this.richTextBox1.Name = "richTextBox1";
         this.richTextBox1.ReadOnly = true;
         this.richTextBox1.Size = new System.Drawing.Size(600, 140);
         this.richTextBox1.TabIndex = 0;
         this.richTextBox1.Text = "Text will be replaced by RTF from CurrentShares.RTF";
         // 
         // CurrentShares
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.richTextBox1);
         this.Controls.Add(this.dataGridView1);
         this.Controls.Add(this.progressBar1);
         this.Controls.Add(this.btnRefresh);
         this.DoubleBuffered = true;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "CurrentShares";
         this.Size = new System.Drawing.Size(600, 475);
         ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.DataGridView dataGridView1;
      private System.Windows.Forms.DataGridViewTextBoxColumn Source;
      private System.Windows.Forms.DataGridViewTextBoxColumn Description;
      private System.Windows.Forms.DataGridViewTextBoxColumn Credentials;
      private System.Windows.Forms.ProgressBar progressBar1;
      private System.Windows.Forms.Button btnRefresh;
      private System.Windows.Forms.RichTextBox richTextBox1;

   }
}
