namespace LiquesceFTPMgr
{
   partial class GridAdvancedSettings
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
          System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GridAdvancedSettings));
          this.button1 = new System.Windows.Forms.Button();
          this.label3 = new System.Windows.Forms.Label();
          this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
          this.SuspendLayout();
          // 
          // button1
          // 
          this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
          this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
          this.button1.Location = new System.Drawing.Point(427, 377);
          this.button1.Name = "button1";
          this.button1.Size = new System.Drawing.Size(75, 23);
          this.button1.TabIndex = 15;
          this.button1.Text = "Save Changes";
          this.button1.UseVisualStyleBackColor = true;
          this.button1.Click += new System.EventHandler(this.button1_Click);
          // 
          // label3
          // 
          this.label3.AutoSize = true;
          this.label3.Location = new System.Drawing.Point(193, 48);
          this.label3.Name = "label3";
          this.label3.Size = new System.Drawing.Size(22, 14);
          this.label3.TabIndex = 4;
          this.label3.Text = "ms";
          // 
          // propertyGrid1
          // 
          this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                      | System.Windows.Forms.AnchorStyles.Left)
                      | System.Windows.Forms.AnchorStyles.Right)));
          this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
          this.propertyGrid1.Name = "propertyGrid1";
          this.propertyGrid1.Size = new System.Drawing.Size(526, 373);
          this.propertyGrid1.TabIndex = 16;
          // 
          // GridAdvancedSettings
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.ClientSize = new System.Drawing.Size(534, 400);
          this.Controls.Add(this.propertyGrid1);
          this.Controls.Add(this.button1);
          this.Controls.Add(this.label3);
          this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
          this.MaximizeBox = false;
          this.MinimizeBox = false;
          this.MinimumSize = new System.Drawing.Size(373, 310);
          this.Name = "GridAdvancedSettings";
          this.Text = "AdvancedSettings";
          this.Load += new System.EventHandler(this.GridAdvancedSettings_Load);
          this.ResumeLayout(false);
          this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.Button button1;
      private System.Windows.Forms.PropertyGrid propertyGrid1;
   }
}