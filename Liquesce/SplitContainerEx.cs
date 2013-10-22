using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Liquesce
{
   /// <summary>
   /// Draw dots, bar, and remove focus after drag
   /// </summary>
   public class SplitContainerEx : SplitContainer
   {
      /// <summary>Determines the thickness of the splitter.</summary>
      [DefaultValue(typeof(int), "5"), Description("Determines the thickness of the splitter.")]
      public virtual new int SplitterWidth
      {
         get { return base.SplitterWidth; }
         set
         {
            if (value < 5)
               value = 5;

            base.SplitterWidth = value;
         }
      }

      private bool _painting;
      public override bool Focused
      {
         get { return _painting ? false : base.Focused; }
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         _painting = true;

         try
         {
            base.OnPaint(e);
            // ControlPaint.DrawGrabHandle(e.Graphics, SplitterRectangle, true, Enabled);

            //paint the three dots'
            Point[] points = new Point[4];
            var w = Width;
            var h = Height;
            var d = SplitterDistance;
            var sW = SplitterWidth;
            int sWC = (sW / 2);

            if (Orientation == Orientation.Horizontal)
            {
               e.Graphics.DrawLine(SystemPens.ControlLightLight, 0, d + sWC-1, w, d + sWC-1);
               //e.Graphics.DrawLine(SystemPens.Control, 0, d + (sW / 2), w, d + (sW / 2));
               e.Graphics.DrawLine(SystemPens.ControlDark, 0, d + sWC+1, w, d + sWC+1);
               points[0] = new Point((w / 2) - 15, d + sWC);
               points[1] = new Point(points[0].X + 10, points[0].Y);
               points[2] = new Point(points[0].X + 20, points[0].Y);
               points[3] = new Point(points[0].X + 30, points[0].Y);
            }
            else
            {
               e.Graphics.DrawLine(SystemPens.ControlLightLight, d + sWC-1, 0, d + sWC-1, h);
               //e.Graphics.DrawLine(SystemPens.ControlDark, d + (sW / 2), 0, d + (sW / 2), h);
               e.Graphics.DrawLine(SystemPens.ControlDark, d + sWC+1, 0, d + sWC+1, h);
               points[0] = new Point(d + sWC, (h / 2) - 15);
               points[1] = new Point(points[0].X, points[0].Y + 10);
               points[2] = new Point(points[0].X, points[0].Y + 20);
               points[3] = new Point(points[0].X, points[0].Y + 30);
            }

            foreach (Point p in points)
            {
               p.Offset(-2, -2);
               e.Graphics.FillEllipse(SystemBrushes.ControlDark, new Rectangle(p, new Size(3, 3)));

               p.Offset(1, 1);
               e.Graphics.FillEllipse(SystemBrushes.ControlLight, new Rectangle(p, new Size(3, 3)));
            }
         }
         finally
         {
            _painting = false;
         }
      }

   }
}
