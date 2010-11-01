using System.Drawing;
using System;
using System.Windows.Forms;
namespace LiquesceTray
{
    partial class DoubleProgressBar
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

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 50);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.panel2.Controls.Add(this.panel1);
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(455, 50);
            this.panel2.TabIndex = 0;
            // 
            // DoubleProgressBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Name = "DoubleProgressBar";
            this.Size = new System.Drawing.Size(683, 68);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        int min = 0;	// Minimum value for progress range
        int max = 100;	// Maximum value for progress range
        int val1 = 0;		// Current progress
        int val2 = 0;

        protected override void OnResize(EventArgs e)
        {
            // Invalidate the control to get a repaint.
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            float percent1 = (float)(val1 - min) / (float)(max - min);
            float percent2 = (float)(val2 - min) / (float)(max - min);
            Rectangle rect = this.ClientRectangle;

            panel1.Top = 0;
            panel2.Top = 1;
            panel1.Left = 0;
            panel2.Left = 1;
            panel1.Height = rect.Height - 2;
            panel2.Height = rect.Height - 2;


            panel1.Width = (int)((float)(rect.Width - 2) * percent1);
            panel2.Width = (int)((float)(rect.Width - 2) * percent2);

            Draw3DBorder(g);
        }

        public int Minimum
        {
            get
            {
                return min;
            }

            set
            {
                // Prevent a negative value.
                if (value < 0)
                {
                    min = 0;
                }
                // Make sure that the minimum value is never set higher than the maximum value.
                if (value > max)
                {
                    min = value;
                    min = value;
                }
                // Ensure value is still in range
                if (val1 < min)
                {
                    val1 = min;
                }

                // Invalidate the control to get a repaint.
                this.Invalidate();
            }
        }

        public int Maximum
        {
            get
            {
                return max;
            }

            set
            {
                // Make sure that the maximum value is never set lower than the minimum value.
                if (value < min)
                {
                    min = value;
                }

                max = value;

                // Make sure that value is still in range.
                if (val1 > max)
                {
                    val1 = max;
                }

                // Invalidate the control to get a repaint.
                this.Invalidate();
            }
        }

        public int Value1
        {
            get
            {
                return val1;
            }

            set
            {
                int oldValue = val1;

                // Make sure that the value does not stray outside the valid range.
                if (value < min)
                {
                    val1 = min;
                }
                else if (value > max)
                {
                    val1 = max;
                }
                else
                {
                    val1 = value;
                }

                // Invalidate the intersection region only.
                this.Invalidate();
            }
        }

        public int Value2
        {
            get
            {
                return val2;
            }

            set
            {
                int oldValue = val2;

                // Make sure that the value does not stray outside the valid range.
                if (value < min)
                {
                    val2 = min;
                }
                else if (value > max)
                {
                    val2 = max;
                }
                else
                {
                    val2 = value;
                }

                // Invalidate the intersection region only.
                this.Invalidate();
            }
        }

        private void Draw3DBorder(Graphics g)
        {
            int PenWidth = (int)Pens.White.Width;

            g.DrawLine(Pens.DarkGray,
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Top),
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Top));
            g.DrawLine(Pens.DarkGray,
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Top),
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Height - PenWidth));
            g.DrawLine(Pens.White,
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Height - PenWidth),
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Height - PenWidth));
            g.DrawLine(Pens.White,
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Top),
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Height - PenWidth));
        }

        private Panel panel1;
        private Panel panel2; 

    }
}
