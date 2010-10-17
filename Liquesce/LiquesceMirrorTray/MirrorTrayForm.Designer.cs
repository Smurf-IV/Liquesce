namespace LiquesceMirrorTray
{
    partial class MirrorTrayForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MirrorTrayForm));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.traytimer = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.ToDoList = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.serviceController1 = new System.ServiceProcess.ServiceController();
            this.timerToDoListRefresh = new System.Windows.Forms.Timer(this.components);
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Liquesce Mirroring App";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // traytimer
            // 
            this.traytimer.Enabled = true;
            this.traytimer.Interval = 500;
            this.traytimer.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ToDoList
            // 
            this.ToDoList.FormattingEnabled = true;
            this.ToDoList.Items.AddRange(new object[] {
            "Eins",
            "Zwei",
            "Drei"});
            this.ToDoList.Location = new System.Drawing.Point(12, 46);
            this.ToDoList.Name = "ToDoList";
            this.ToDoList.Size = new System.Drawing.Size(463, 186);
            this.ToDoList.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(127, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "adsfasdfas";
            // 
            // serviceController1
            // 
            this.serviceController1.MachineName = "127.0.0.1";
            this.serviceController1.ServiceName = "LiquesceSvc";
            // 
            // timerToDoListRefresh
            // 
            this.timerToDoListRefresh.Interval = 2000;
            this.timerToDoListRefresh.Tick += new System.EventHandler(this.timerToDoListRefresh_Tick);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(375, 12);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(100, 23);
            this.progressBar.TabIndex = 3;
            // 
            // MirrorTrayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 245);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ToDoList);
            this.Controls.Add(this.button1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MirrorTrayForm";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TrayForm_FormClosing);
            this.Load += new System.EventHandler(this.TrayForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.Timer traytimer;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox ToDoList;
        private System.Windows.Forms.Label label1;
        private System.ServiceProcess.ServiceController serviceController1;
        private System.Windows.Forms.Timer timerToDoListRefresh;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}

