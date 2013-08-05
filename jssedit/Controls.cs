using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace jssedit
{

    class GraphControl : Control
    {

        public GraphControl()
        {

        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // Call the OnPaint method of the base class.
            base.OnPaint(pe);

            var g = pe.Graphics;

            // Declare and instantiate a new pen.
            System.Drawing.Pen myPen = new System.Drawing.Pen(Color.White, 1);

            // Draw an aqua rectangle in the rectangle represented by the control.
            //g.DrawRectangle(myPen, new Rectangle(20, 20, 50, 50));


            // Create a GraphicsPath object.
            GraphicsPath myPath = new GraphicsPath();

            // Set up and call AddArc, and close the figure.
            Rectangle rect = new Rectangle(20, 20, 50, 100);
            int cw = 10;
            myPath.StartFigure();
            myPath.AddLine(rect.Right, rect.Top + cw, rect.Right, rect.Bottom - cw);
            myPath.AddArc(new Rectangle(rect.Right - cw, rect.Bottom - cw, cw, cw), 0, 90);
            myPath.AddLine(rect.Right - cw, rect.Bottom, rect.Left + cw, rect.Bottom);
            myPath.AddArc(new Rectangle(rect.Left, rect.Bottom - cw, cw, cw), 90, 90);
            myPath.AddLine(rect.Left, rect.Bottom - cw, rect.Left, rect.Top + cw);
            myPath.AddArc(new Rectangle(rect.Left, rect.Top, cw, cw), 180, 90);
            myPath.AddLine(rect.Left + cw, rect.Top, rect.Right - cw, rect.Top);
            myPath.AddArc(new Rectangle(rect.Right - cw, rect.Top , cw, cw), 270, 90);
            myPath.CloseFigure();

            // Draw the path to screen.

            var br = new SolidBrush(Color.AliceBlue);

            g.FillPath(br, myPath);
            g.DrawPath(new Pen(Color.Red, 1), myPath);

        }
    }

}