using System.Drawing;
using System;
using System.Windows.Forms;
namespace LiquesceTray
{
    partial class DoubleProgressBar
    {
        Color COLOR_BAR1 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
        Color COLOR_BAR2 = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
        Color COLOR_FREE_SPACE = Control.DefaultBackColor;
        Color COLOR_FREE_SPACE_PRIOR1 = Color.Lime;
        Color COLOR_FREE_SPACE_PRIOR2 = Color.LimeGreen;

        Pen PEN_BORDER_DARK = Pens.DarkGray;
        Pen PEN_BORDER_LIGHT = Pens.White;
        Pen PEN_BORDER_ERROR = Pens.Red;
        Pen PEN_BORDER_WARN = Pens.Orange;


        public enum ErrorStatusType
        {
            NoError,
            Warn,
            Error
        }

        public enum RateType
        {
            No,
            Positive,
            Negative
        }

        public enum WriteMarkType
        {
            No,
            Priority1,
            Priority2
        }

        int min = 0;	// Minimum value for progress range
        int max = 100;	// Maximum value for progress range
        int val1 = 0;		// Current progress
        int val2 = 0;

        ErrorStatusType errorState = ErrorStatusType.NoError;

        RateType rate = RateType.No;

        WriteMarkType writemark = WriteMarkType.No;


        
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
            this.labelChange = new TransparentLabel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = COLOR_BAR1;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 50);
            this.panel1.TabIndex = 0;
            // 
            // labelChange
            // 
            this.labelChange.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelChange.Location = new System.Drawing.Point(498, 17);
            this.labelChange.Margin = new System.Windows.Forms.Padding(0);
            this.labelChange.Name = "labelChange";
            this.labelChange.Size = new System.Drawing.Size(12, 12);
            this.labelChange.TabIndex = 1;
            this.labelChange.Text = "+";
            this.labelChange.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.BackColor = COLOR_BAR2;
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
            this.Controls.Add(this.labelChange);
            this.Controls.Add(this.panel2);
            this.Name = "DoubleProgressBar";
            this.Size = new System.Drawing.Size(683, 68);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

            ToolTip_panel1 = new System.Windows.Forms.ToolTip();
            ToolTip_panel1.SetToolTip(this.panel1, "Data");

            ToolTip_free = new System.Windows.Forms.ToolTip();
            ToolTip_free.SetToolTip(this, "Free Space");

        }

        #endregion




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

            if (writemark == WriteMarkType.No)
            {
                this.BackColor = COLOR_FREE_SPACE;
                ToolTip_free.SetToolTip(this, "Free Space");
            }
            else if (writemark == WriteMarkType.Priority1)
            {
                this.BackColor = COLOR_FREE_SPACE_PRIOR1;
                ToolTip_free.SetToolTip(this, "Free Space - First disk to write on.");
            }
            else if (writemark == WriteMarkType.Priority2)
            {
                this.BackColor = COLOR_FREE_SPACE_PRIOR2;
                ToolTip_free.SetToolTip(this, "Free Space - Alternative disk to write on.");
            }


            if (errorState != ErrorStatusType.NoError)
            {
                panel1.Top = 0;
                panel2.Top = 2;
                panel1.Left = 0;
                panel2.Left = 2;
                panel1.Height = rect.Height - 4;
                panel2.Height = rect.Height - 4;


                panel1.Width = (int)((float)(rect.Width - 2) * percent1) - 2;
                panel2.Width = (int)((float)(rect.Width - 2) * percent2) - 2;
            }
            else
            {
                panel1.Top = 0;
                panel2.Top = 1;
                panel1.Left = 0;
                panel2.Left = 1;
                panel1.Height = rect.Height - 2;
                panel2.Height = rect.Height - 2;


                panel1.Width = (int)((float)(rect.Width - 2) * percent1);
                panel2.Width = (int)((float)(rect.Width - 2) * percent2);
            }

            labelChange.Width = 20;
            labelChange.Height = rect.Height - 5;
            labelChange.Top = 1;

            if (rate == RateType.No)
            {
                labelChange.Visible = false;
            }
            else if (rate == RateType.Positive)
            {
                labelChange.Visible = true;
                labelChange.Text = ">";
                if (labelChange.Width + panel2.Width < rect.Width - 2)
                {
                    labelChange.Left = panel2.Width + 1;
                    labelChange.ForeColor = COLOR_BAR1;
                }
                else
                {
                    labelChange.Left = panel2.Width - labelChange.Width + 1;
                    labelChange.ForeColor = COLOR_FREE_SPACE;
                }
            }
            else if (rate == RateType.Negative)
            {
                labelChange.Visible = true;
                labelChange.Text = "<";
                if (labelChange.Width > panel2.Width)
                {
                    labelChange.Left = panel2.Width + 1;
                    labelChange.ForeColor = COLOR_BAR1;
                }
                else
                {
                    labelChange.Left = panel2.Width - labelChange.Width + 1;
                    labelChange.ForeColor = COLOR_FREE_SPACE;
                }
            }

            labelChange.Invalidate();

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

                if (val1 != oldValue)
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

                if (val2 != oldValue)
                    // Invalidate the intersection region only.
                    this.Invalidate();
            }
        }

        public ErrorStatusType ErrorStatus
        {
            get
            {
                return errorState;
            }

            set
            {
                ErrorStatusType old = errorState;
                errorState = value;

                if (errorState != old)
                    // Invalidate the intersection region only.
                    this.Invalidate();
            }
        }

        public RateType Rate
        {
            get
            {
                return rate;
            }

            set
            {
                RateType old = rate;
                rate = value;

                if (rate != old)
                    // Invalidate the intersection region only.
                    this.Invalidate();
            }
        }

        // marks the disk as the next for writing
        public WriteMarkType WriteMark
        {
            get
            {
                return writemark;
            }

            set
            {
                WriteMarkType old = writemark;
                writemark = value;

                if (writemark != old)
                    // Invalidate the intersection region only.
                    this.Invalidate();
            }
        }

        private void Draw3DBorder(Graphics g)
        {
            int PenWidth = (int)Pens.White.Width;

            Pen dark;
            Pen light;

            if (errorState == ErrorStatusType.Warn)
            {
                dark = PEN_BORDER_WARN;
                light = PEN_BORDER_WARN;
            }
            else if (errorState == ErrorStatusType.Error)
            {
                dark = PEN_BORDER_ERROR;
                light = PEN_BORDER_ERROR;
            }
            else
            {
                dark = PEN_BORDER_DARK;
                light = PEN_BORDER_LIGHT;
            }

            g.DrawLine(dark,
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Top),
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Top));
            g.DrawLine(dark,
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Top),
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Height - PenWidth));
            g.DrawLine(light,
                new Point(this.ClientRectangle.Left, this.ClientRectangle.Height - PenWidth),
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Height - PenWidth));
            g.DrawLine(light,
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Top),
                new Point(this.ClientRectangle.Width - PenWidth, this.ClientRectangle.Height - PenWidth));

            if (errorState == ErrorStatusType.Warn || errorState == ErrorStatusType.Error)
            {
                g.DrawLine(dark,
                    new Point(this.ClientRectangle.Left + 1, this.ClientRectangle.Top + 1),
                    new Point(this.ClientRectangle.Width - PenWidth - 1, this.ClientRectangle.Top + 1));
                g.DrawLine(dark,
                    new Point(this.ClientRectangle.Left + 1, this.ClientRectangle.Top + 1),
                    new Point(this.ClientRectangle.Left + 1, this.ClientRectangle.Height - PenWidth - 1));
                g.DrawLine(light,
                    new Point(this.ClientRectangle.Left + 1, this.ClientRectangle.Height - PenWidth - 1),
                    new Point(this.ClientRectangle.Width - PenWidth - 1, this.ClientRectangle.Height - PenWidth - 1));
                g.DrawLine(light,
                    new Point(this.ClientRectangle.Width - PenWidth - 1, this.ClientRectangle.Top + 1),
                    new Point(this.ClientRectangle.Width - PenWidth - 1, this.ClientRectangle.Height - PenWidth - 1));
            }
        }

        private Panel panel1;
        private Panel panel2;
        ToolTip ToolTip_panel1;
        ToolTip ToolTip_free;
        private TransparentLabel labelChange; 


    }
}
