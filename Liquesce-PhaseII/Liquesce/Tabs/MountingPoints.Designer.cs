namespace Liquesce.Tabs
{
   partial class MountingPoints
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
         this.listExistingMounts = new System.Windows.Forms.ListBox();
         this.pnlStart = new System.Windows.Forms.Panel();
         this.btnNew = new Shared.CommandLinkButton();
         this.btnEdit = new Shared.CommandLinkButton();
         this.btnDelete = new Shared.CommandLinkButton();
         this.edit1 = new Liquesce.Mounting.Edit();
         this.pnlStart.SuspendLayout();
         this.SuspendLayout();
         // 
         // listExistingMounts
         // 
         this.listExistingMounts.FormattingEnabled = true;
         this.listExistingMounts.IntegralHeight = false;
         this.listExistingMounts.ItemHeight = 14;
         this.listExistingMounts.Location = new System.Drawing.Point(402, 91);
         this.listExistingMounts.Name = "listExistingMounts";
         this.listExistingMounts.ScrollAlwaysVisible = true;
         this.listExistingMounts.Size = new System.Drawing.Size(154, 152);
         this.listExistingMounts.TabIndex = 3;
         this.listExistingMounts.SelectedIndexChanged += new System.EventHandler(this.listExistingMounts_SelectedIndexChanged);
         // 
         // pnlStart
         // 
         this.pnlStart.Controls.Add(this.btnNew);
         this.pnlStart.Controls.Add(this.btnEdit);
         this.pnlStart.Controls.Add(this.btnDelete);
         this.pnlStart.Controls.Add(this.listExistingMounts);
         this.pnlStart.Dock = System.Windows.Forms.DockStyle.Fill;
         this.pnlStart.Location = new System.Drawing.Point(0, 0);
         this.pnlStart.Name = "pnlStart";
         this.pnlStart.Size = new System.Drawing.Size(600, 475);
         this.pnlStart.TabIndex = 4;
         // 
         // btnNew
         // 
         this.btnNew.ButtonDepress = ((sbyte)(2));
         this.btnNew.Enabled = false;
         this.btnNew.HighlightFillAlpha = ((byte)(250));
         this.btnNew.HighlightFillAlphaMouse = ((byte)(125));
         this.btnNew.HighlightFillAlphaNormal = ((byte)(75));
         this.btnNew.HighlightWidth = 2F;
         this.btnNew.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_Nfs_mount;
         this.btnNew.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnNew.Location = new System.Drawing.Point(4, 4);
         this.btnNew.Name = "btnNew";
         this.btnNew.Rounding = 10F;
         this.btnNew.Size = new System.Drawing.Size(375, 65);
         this.btnNew.Subscript = "   Create a new mounting point.";
         this.btnNew.TabIndex = 0;
         this.btnNew.Text = "&New";
         this.btnNew.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnNew.UseVisualStyleBackColor = true;
         this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
         // 
         // btnEdit
         // 
         this.btnEdit.ButtonDepress = ((sbyte)(2));
         this.btnEdit.HighlightFillAlpha = ((byte)(250));
         this.btnEdit.HighlightFillAlphaMouse = ((byte)(125));
         this.btnEdit.HighlightFillAlphaNormal = ((byte)(75));
         this.btnEdit.HighlightWidth = 2F;
         this.btnEdit.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_Nfs_unmount;
         this.btnEdit.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnEdit.Location = new System.Drawing.Point(4, 91);
         this.btnEdit.Name = "btnEdit";
         this.btnEdit.Rounding = 10F;
         this.btnEdit.Size = new System.Drawing.Size(375, 65);
         this.btnEdit.Subscript = "   Change the selected mount point details.";
         this.btnEdit.TabIndex = 1;
         this.btnEdit.Text = "&Edit";
         this.btnEdit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnEdit.UseVisualStyleBackColor = true;
         this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
         // 
         // btnDelete
         // 
         this.btnDelete.ButtonDepress = ((sbyte)(2));
         this.btnDelete.Enabled = false;
         this.btnDelete.HighlightFillAlpha = ((byte)(250));
         this.btnDelete.HighlightFillAlphaMouse = ((byte)(125));
         this.btnDelete.HighlightFillAlphaNormal = ((byte)(75));
         this.btnDelete.HighlightWidth = 2F;
         this.btnDelete.Image = global::Liquesce.Properties.Resources.Umut_Pulat_Tulliana_2_K_ppp;
         this.btnDelete.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnDelete.Location = new System.Drawing.Point(3, 178);
         this.btnDelete.Name = "btnDelete";
         this.btnDelete.Rounding = 10F;
         this.btnDelete.Size = new System.Drawing.Size(375, 65);
         this.btnDelete.Subscript = "   Delete the selected mount point.";
         this.btnDelete.TabIndex = 2;
         this.btnDelete.Text = "&Delete";
         this.btnDelete.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         this.btnDelete.UseVisualStyleBackColor = true;
         this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
         // 
         // edit1
         // 
         this.edit1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.edit1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.edit1.Location = new System.Drawing.Point(0, 0);
         this.edit1.Name = "edit1";
         this.edit1.Size = new System.Drawing.Size(600, 475);
         this.edit1.TabIndex = 4;
         this.edit1.Visible = false;
         // 
         // MountingPoints
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.pnlStart);
         this.Controls.Add(this.edit1);
         this.DoubleBuffered = true;
         this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.Name = "MountingPoints";
         this.Size = new System.Drawing.Size(600, 475);
         this.pnlStart.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private Shared.CommandLinkButton btnNew;
      private Shared.CommandLinkButton btnEdit;
      private Shared.CommandLinkButton btnDelete;
      private System.Windows.Forms.ListBox listExistingMounts;
      private System.Windows.Forms.Panel pnlStart;
      private Mounting.Edit edit1;

   }
}