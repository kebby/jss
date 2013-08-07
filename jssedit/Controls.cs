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

        public void SetGraph(Graph gr)
        {
            MyGraph = gr;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {

            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // Call the OnPaint method of the base class.
            base.OnPaint(pe);

            if (MyGraph == null)
                return;

            var g = pe.Graphics;

            if (Dirty)
            {
                DoLayout(g);
                Dirty = false;
            }

            foreach (var cable in Cables)
                PaintCable(g, cable);

            foreach (var mod in Modules)
                PaintModule(g,mod);

        }


        void DoLayout(Graphics g)
        {
            Modules = new List<ModuleInfo>();
            foreach (var mod in MyGraph.Modules)
            {
                var hw = g.MeasureString(mod.Name, ModHeadFnt);
                var x = mod.X;
                var y = mod.Y;
                var w = (int)(hw.Width + 15);
                var h = ParamY + ParamH * (mod.Inputs.Length - 1);

                Modules.Add(new ModuleInfo
                {
                    Mod = mod,
                    Rect = new Rectangle(x,y,w,h),
                });
            }

            Cables = new List<CableInfo>();
            foreach (var mod in Modules)
            {
                for (int i = 0; i < mod.Mod.Inputs.Length; i++)
                {
                    var srcm = mod.Mod.Inputs[i];
                    if (srcm != null)
                    {
                        var src = Modules.First(mi => mi.Mod == srcm);

                        Cables.Add(new CableInfo
                        {
                            Src = src,
                            Dest = mod,
                            Index = i,
                        });
                        
                    }
                }

            }
        }

        void PaintCable(Graphics g, CableInfo c)
        {
            var p1 = new Point();
            var p2 = new Point();
            var p3 = new Point();
            var p4 = new Point();

            p1.X = c.Src.Rect.Right;
            p1.Y = c.Src.Rect.Top + IOPinY;
            p2.Y = p1.Y;           

            p4.X = c.Dest.Rect.Left;
            if (c.Index > 0)
                p4.Y = c.Dest.Rect.Top + ParamY + ParamPinY + ParamH * (c.Index - 1);
            else
                p4.Y = c.Dest.Rect.Top + IOPinY;
            p3.Y = p4.Y;

            if (p4.Y > p1.Y)
            {
                p2.X = p3.X = (2 * p4.X + p1.X) / 3;
            }
            else
            {
                p2.X = p3.X = (2 * p1.X + p4.X) / 3;
            }

            var pen = c.Src.Mod.Definition.OutChannels>1 ? CableStereo: CableMono;
            g.DrawLine(pen, p1, p2);
            g.DrawLine(pen, p2, p3);
            g.DrawLine(pen, p3, p4);
        }

        void PaintModule(Graphics g, ModuleInfo mi)
        {
            
            var m = mi.Mod;
            var rect = mi.Rect;
            var x = rect.Left;
            var y = rect.Top;
            var w = rect.Width;
            var h = rect.Height;

            // draw rectangle
            PaintRoundedRect(g, rect, 10, ModBack, ModBorder);

            // draw labels
            g.DrawString(m.Name, ModHeadFnt, ModText, new Rectangle(x + 5, y + 5, w - 10, 20), ModHeadFmt);
            for (int i = 0; i < m.Params.Length; i++)
                g.DrawString(m.Definition.ParamNames[i], ModParamFnt, ModText, new PointF(x + 2, y + ParamY + i * ParamH));

            // draw pins
            if (m.Definition.InChannels > 0)
            {
                g.FillEllipse((m.Definition.InChannels>1 ? ModPinStereo : ModPinMono),
                              x - 5, y + IOPinY -4, 7, 7);
            }
            if (m.Definition.OutChannels > 0)
            {
                g.FillEllipse((m.Definition.OutChannels > 1 ? ModPinStereo : ModPinMono),
                              x + w - 2, y + IOPinY -4, 7, 7);
            }
            for (int i = 0; i < m.Params.Length; i++)
            {
                g.FillEllipse(ModPinMono, x - 4, y + ParamY + i * ParamH + ParamPinY -2, 5, 5);
            }
        }

        void PaintRoundedRect(Graphics g, Rectangle rect, int radius, Brush fill, Pen outline = null)
        {
            using (var myPath = new GraphicsPath())
            {
                myPath.StartFigure();
                myPath.AddArc(new Rectangle(rect.Right - radius, rect.Bottom - radius, radius, radius), 0, 90);
                myPath.AddArc(new Rectangle(rect.Left, rect.Bottom - radius, radius, radius), 90, 90);
                myPath.AddArc(new Rectangle(rect.Left, rect.Top, radius, radius), 180, 90);
                myPath.AddArc(new Rectangle(rect.Right - radius, rect.Top, radius, radius), 270, 90);
                myPath.CloseFigure();

                if (fill != null) g.FillPath(fill, myPath);
                if (outline != null) g.DrawPath(outline, myPath);
            }
        }


        class ModuleInfo
        {
            public Module Mod;
            public Rectangle Rect;
        };

        class CableInfo
        {
            public ModuleInfo Src;
            public ModuleInfo Dest;
            public int Index;
        };

        bool Dirty = true;
        List<ModuleInfo> Modules;
        List<CableInfo> Cables;

        Font ModHeadFnt = new Font(FontFamily.GenericSansSerif, 8);
        Font ModParamFnt = new Font(FontFamily.GenericSansSerif, 7);

        Brush ModBack = new SolidBrush(Color.DarkSlateBlue);
        Brush ModText = new SolidBrush(Color.White);

        Brush ModPinMono   = new SolidBrush(Color.PaleTurquoise);
        Brush ModPinStereo = new SolidBrush(Color.White);

        Pen ModBorder = new Pen(Color.LightSteelBlue,1);
        Pen ModBorderSelected = new Pen(Color.White, 3);

        Pen CableMono = new Pen(Color.PaleTurquoise, 2);
        Pen CableStereo = new Pen(Color.White, 2);

        const int ParamY = 30;
        const int ParamH = 15;

        const int IOPinY = 11;
        const int ParamPinY = 6;

        StringFormat ModHeadFmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
        };

        Graph MyGraph;
    }

}