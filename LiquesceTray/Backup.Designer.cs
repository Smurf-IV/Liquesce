namespace LiquesceTray
{
    partial class Backup
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Backup));
            this.buttonConsistency = new System.Windows.Forms.Button();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.textCurrent = new System.Windows.Forms.TextBox();
            this.buttonRemoveInconsistent = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.listInconsistent = new System.Windows.Forms.ListBox();
            this.listMissing = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonRemoveMissing = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonConsistency
            // 
            this.buttonConsistency.Location = new System.Drawing.Point(577, 480);
            this.buttonConsistency.Name = "buttonConsistency";
            this.buttonConsistency.Size = new System.Drawing.Size(162, 23);
            this.buttonConsistency.TabIndex = 1;
            this.buttonConsistency.Text = "Check Backup Consistency";
            this.buttonConsistency.UseVisualStyleBackColor = true;
            this.buttonConsistency.Click += new System.EventHandler(this.buttonConsistency_Click);
            // 
            // progress
            // 
            this.progress.Enabled = false;
            this.progress.Location = new System.Drawing.Point(11, 481);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(560, 22);
            this.progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progress.TabIndex = 2;
            // 
            // textCurrent
            // 
            this.textCurrent.Location = new System.Drawing.Point(11, 454);
            this.textCurrent.Name = "textCurrent";
            this.textCurrent.ReadOnly = true;
            this.textCurrent.Size = new System.Drawing.Size(804, 20);
            this.textCurrent.TabIndex = 3;
            // 
            // buttonRemoveInconsistent
            // 
            this.buttonRemoveInconsistent.Enabled = false;
            this.buttonRemoveInconsistent.Location = new System.Drawing.Point(618, 270);
            this.buttonRemoveInconsistent.Name = "buttonRemoveInconsistent";
            this.buttonRemoveInconsistent.Size = new System.Drawing.Size(197, 23);
            this.buttonRemoveInconsistent.TabIndex = 4;
            this.buttonRemoveInconsistent.Text = "Remove Inconsistent Backup Files";
            this.buttonRemoveInconsistent.UseVisualStyleBackColor = true;
            this.buttonRemoveInconsistent.Click += new System.EventHandler(this.buttonRemoveInconsistent_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(691, 117);
            this.label1.TabIndex = 5;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // listInconsistent
            // 
            this.listInconsistent.FormattingEnabled = true;
            this.listInconsistent.Location = new System.Drawing.Point(11, 156);
            this.listInconsistent.Name = "listInconsistent";
            this.listInconsistent.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.listInconsistent.Size = new System.Drawing.Size(804, 108);
            this.listInconsistent.TabIndex = 6;
            // 
            // listMissing
            // 
            this.listMissing.FormattingEnabled = true;
            this.listMissing.Location = new System.Drawing.Point(11, 314);
            this.listMissing.Name = "listMissing";
            this.listMissing.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.listMissing.Size = new System.Drawing.Size(804, 108);
            this.listMissing.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 140);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(140, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Inconsistent Files or Folders:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 298);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(118, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Missing Files or Folders:";
            // 
            // buttonRemoveMissing
            // 
            this.buttonRemoveMissing.Enabled = false;
            this.buttonRemoveMissing.Location = new System.Drawing.Point(618, 428);
            this.buttonRemoveMissing.Name = "buttonRemoveMissing";
            this.buttonRemoveMissing.Size = new System.Drawing.Size(197, 23);
            this.buttonRemoveMissing.TabIndex = 4;
            this.buttonRemoveMissing.Text = "Remove Outdated Backup Files";
            this.buttonRemoveMissing.UseVisualStyleBackColor = true;
            this.buttonRemoveMissing.Click += new System.EventHandler(this.buttonRemoveMissing_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Enabled = false;
            this.buttonCancel.Location = new System.Drawing.Point(745, 480);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(70, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // Backup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 515);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listMissing);
            this.Controls.Add(this.listInconsistent);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonRemoveMissing);
            this.Controls.Add(this.buttonRemoveInconsistent);
            this.Controls.Add(this.textCurrent);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonConsistency);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Backup";
            this.Text = "Liquesce .backup Consistency Checker";
            this.Load += new System.EventHandler(this.Backup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.TextBox textCurrent;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.ListBox listMissing;
        public System.Windows.Forms.ListBox listInconsistent;
        public System.Windows.Forms.ProgressBar progress;
        public System.Windows.Forms.Button buttonConsistency;
        public System.Windows.Forms.Button buttonRemoveInconsistent;
        public System.Windows.Forms.Button buttonRemoveMissing;
        public System.Windows.Forms.Button buttonCancel;
    }
}