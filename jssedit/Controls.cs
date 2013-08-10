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
            DoubleBuffered = true;
        }

        interface IThing
        {
            int Priority { get; }
            void Paint(Graphics g, GraphControl c, bool hover, bool select);
            Rectangle HitRect { get; }
            bool HitTest2(Point p);
            bool IsSame(IThing t);
        }

        class TModule : IThing
        {
            public int Priority { get { return 1; } }

            public void Paint(Graphics g, GraphControl c, bool hover, bool select)
            {
                var rect = HitRect;
                var x = rect.Left;
                var y = rect.Top;
                var w = rect.Width;
                var h = rect.Height;

                // draw rectangle
                PaintRoundedRect(g, rect, 10, c.ModBack, select ? c.ModSelectedPen : hover ? c.ModHoveredPen:c.ModBorder);

                // draw labels
                g.DrawString(Mod.Name, c.ModHeadFnt, c.ModText, new Rectangle(x + 5, y + 5, w - 10, 20), c.ModHeadFmt);
                for (int i = 0; i < Mod.Params.Length; i++)
                    g.DrawString(Mod.Definition.ParamNames[i], c.ModParamFnt, c.ModText, new PointF(x + 2, y + ParamY + i * ParamH));
            }

            public Rectangle HitRect { get; private set; }

            public bool HitTest2(Point p) { return true;  }

            public bool IsSame(IThing t) { return t is TModule && (t as TModule).Mod == Mod; }

            public Module Mod;

            public TModule(Module mod, GraphControl c, Graphics g)
            {
                var hw = g.MeasureString(mod.Name, c.ModHeadFnt);
                var x = mod.X;
                var y = mod.Y;
                var w = (int)(hw.Width + 15);
                var h = ParamY + ParamH * (mod.Inputs.Length - 1);

                Mod = mod;
                HitRect = new Rectangle(x, y, w, h);
            }

        }

        class TPin : IThing
        {
            public int Priority { get { return 2; } }

            public void Paint(Graphics g, GraphControl c, bool hover, bool select)
            {
                g.FillEllipse((hover || select) ? c.ModSelectedBrush : Brush, HitRect);
            }

            public Rectangle HitRect { get; private set; }

            public bool HitTest2(Point p) { return true; }

            public bool IsSame(IThing t) { TPin p = t as TPin; return p != null && p.Module.IsSame(Module) && p.IsOutput == IsOutput && p.Index == Index; }

            public TModule Module;
            public bool IsOutput;
            public int Index;

            public Brush Brush;

            public TPin(TModule mod, bool output, int index, GraphControl c)
            {
                Module = mod;
                IsOutput = output;
                Index = index;

                if (output)
                {
                    HitRect = new Rectangle(mod.HitRect.Right - 2, mod.HitRect.Top + IOPinY - 4, 7, 7);
                    Brush = mod.Mod.Definition.OutChannels > 1 ? c.ModPinStereo : c.ModPinMono;
                }
                else if (Index>0)
                {
                    HitRect = new Rectangle(mod.HitRect.Left - 4, mod.HitRect.Top + ParamY + (index - 1) * ParamH + ParamPinY - 2, 5, 5);
                    Brush = c.ModPinMono;
                }
                else
                {
                    HitRect = new Rectangle(mod.HitRect.Left - 5, mod.HitRect.Top + IOPinY - 4, 7, 7);
                    Brush = mod.Mod.Definition.InChannels > 1 ? c.ModPinStereo : c.ModPinMono;
                }
            }
        }


        class TCable : IThing
        {
            public int Priority { get { return 0; } }

            public void Paint(Graphics g, GraphControl c, bool hover, bool select)
            {
                g.DrawLines(select ? c.ModSelectedPen : Pen, Points);
            }

            public Rectangle HitRect { get; private set; }

            public bool HitTest2(Point p) 
            {
                if (Points == null) return false;
                for (int i = 0; i < Points.Length - 1; i++)
                {
                    if (DistanceFromSegment(p, Points[i], Points[i+1]) < 3.0f)
                        return true;
                }
                return false;
            }

            public bool IsSame(IThing t) { TCable c = t as TCable; return c != null && c.Src.IsSame(Src) && c.Dest.IsSame(Dest) && c.Index == Index; }

            public TModule Src;
            public TModule Dest;
            public int Index;

            public Point[] Points;
            public Pen Pen;

            void Layout(Point a, Point b, int topa, int bottoma, int topb, int bottomb)
            {
                Point[] p;

                if (b.X > a.X || (topa>=bottoma))
                {
                    p = new Point[4];
                    p[0] = a; p[3] = b;
                    p[1].Y = p[0].Y;
                    p[2].Y = p[3].Y;

                    if (p[3].Y > p[0].Y)
                        p[1].X = p[2].X = (2 * p[3].X + p[0].X) / 3;
                    else
                        p[1].X = p[2].X = (2 * p[0].X + p[3].X) / 3;
                }
                else
                {
                    p = new Point[6];
                    p[0] = a; p[5] = b;

                    p[1].X = p[0].X + 10;
                    p[1].Y = p[0].Y;

                    topa -= 10;
                    bottoma += 10;

                    var tw = Math.Abs(a.Y - topa) + Math.Abs(topa - b.Y);
                    var bw = Math.Abs(bottoma - a.Y) + Math.Abs(bottoma - b.Y);

                    p[2].X = p[1].X;
                    if (tw < bw)
                        p[2].Y = topa;
                    else
                        p[2].Y = bottoma;

                    p[3].Y = p[2].Y;
                    if (bottomb > topb)
                    {
                        if (p[3].Y > p[5].Y)
                            p[3].X = b.X - (bottomb - p[5].Y) - 10;
                        else
                            p[3].X = b.X - (p[5].Y - topb) - 10;
                    }
                    else
                        p[3].X = b.X - 10;

                    p[4].X = p[3].X;
                    p[4].Y = p[5].Y;
                }
               
                Points = p;
            }

            public TCable(TModule src, TModule dest, int index, GraphControl c)
            {
                Src = src;
                Dest = dest;
                Index = index;

                var pa = new Point(Src.HitRect.Right,Src.HitRect.Top + IOPinY);
                var pb = new Point(Dest.HitRect.Left,(index>0)?(Dest.HitRect.Top + ParamY + ParamPinY + ParamH * (Index - 1)):(Dest.HitRect.Top + IOPinY));

                Layout(pa, pb, Src.HitRect.Top, Src.HitRect.Bottom, Dest.HitRect.Top, Dest.HitRect.Bottom);
                Pen = Src.Mod.Definition.OutChannels > 1 ? c.CableStereo : c.CableMono;
                HitRect = RectFromPoints(Points, 2);
            }

            public TCable(TModule src, Point b, GraphControl c)
            {
                Src = src;
                var pa = new Point(Src.HitRect.Right, Src.HitRect.Top + IOPinY);
                Layout(pa, b, Src.HitRect.Top, Src.HitRect.Bottom, 0, 0);
                HitRect = RectFromPoints(Points, 2);
            }

            public TCable(Point a, TModule dest, int index, GraphControl c)
            {
                Dest = dest;
                Index = index;
                var pb = new Point(Dest.HitRect.Left, (index > 0) ? (Dest.HitRect.Top + ParamY + ParamPinY + ParamH * (Index - 1)) : (Dest.HitRect.Top + IOPinY));
                Layout(a, pb, 0, 0, Dest.HitRect.Top, Dest.HitRect.Bottom);
                HitRect = RectFromPoints(Points, 2);
            }

        }


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
                Selected = Things.FirstOrDefault(t => t.IsSame(Selected));
                Hovered = Things.FirstOrDefault(t => t.IsSame(Hovered));
            }

            foreach (var thing in Things.OrderBy(t => t.Priority + (t == Selected ? 100 : 0)))
                thing.Paint(g, this, thing==Hovered, thing==Selected);

            if (DragCable != null)
                DragCable.Paint(g, this, true, true);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            SetHovered(null);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            SetHovered(Hit(e.Location));

            switch (Drag)
            {
                case DragOp.Module:
                    {
                        var tmod = Selected as TModule;

                        tmod.Mod.X = Math.Max(0, e.X - DragOffset.X);
                        tmod.Mod.Y = Math.Max(0, e.Y - DragOffset.Y);

                        Dirty = true;
                        Invalidate();
                    }
                    break;
                case DragOp.CableFrom:
                    {
                        var tpin = Selected as TPin;
                        DragCable = new TCable(tpin.Module, e.Location, this);
                        Invalidate();
                    }
                    break;
                case DragOp.CableTo:
                    {
                        var tpin = Selected as TPin;
                        DragCable = new TCable(e.Location, tpin.Module, tpin.Index, this);
                        Invalidate();
                    }
                    break;
            }

        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            SetSelected(Hit(e.Location));

            if (e.Button == MouseButtons.Left)
            {
                if (Selected is TModule)
                {
                    var tmod = (Selected as TModule);
                    Drag = DragOp.Module;
                    DragOffset.X = e.X - tmod.Mod.X;
                    DragOffset.Y = e.Y - tmod.Mod.Y;
                }
                else if (Selected is TPin)
                {
                    var tpin = (Selected as TPin);
                    if (tpin.IsOutput)
                        Drag = DragOp.CableFrom;
                    else if (tpin.Module.Mod.Inputs[tpin.Index] == null)
                        Drag = DragOp.CableTo;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (Selected is TModule || Selected is TCable)
                {
                    var item = new MenuItem("Delete", (o, ev) => Delete());
                    var menu = new ContextMenu(new[] { item });
                    menu.Show(this, e.Location);
                }
                else if (Selected == null)
                {
                    var items = GetModuleMenuItems("", n => AddModule(n, e.Location));
                    var menu = new ContextMenu(items);
                    menu.Show(this, e.Location);
                }
            }
           
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            switch (Drag)
            {
                case DragOp.CableFrom:
                    {
                        var src = Selected as TPin;
                        var dest = Hovered as TPin;
                        if (dest != null && !dest.IsOutput && dest.Module.Mod.Inputs[dest.Index] == null)
                        {
                            try
                            {
                                MyGraph.Connect(src.Module.Mod, dest.Module.Mod,
                                    (dest.Index > 0 ? dest.Module.Mod.Definition.ParamNames[dest.Index - 1] : null));
                            }
                            catch (ModelException me)
                            {
                                MessageBox.Show(me.Message);
                            }
                            Dirty = true;
                        }
                        DragCable = null;
                        Invalidate();                    
                    }
                    break;
                case DragOp.CableTo:
                    {
                        var src = Hovered as TPin;
                        var dest = Selected as TPin;
                        if (src != null && src.IsOutput)
                        {
                            try
                            {
                                MyGraph.Connect(src.Module.Mod, dest.Module.Mod,
                                    (dest.Index > 0 ? dest.Module.Mod.Definition.ParamNames[dest.Index - 1] : null));
                            }
                            catch (ModelException me)
                            {
                                MessageBox.Show(me.Message);
                            }
                            Dirty = true;
                        }
                        DragCable = null;
                        Invalidate();                    
                    }
                    break;
            }
            Drag = DragOp.None;

        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Delete)
                Delete();
        }


        void Delete()
        {
            if (Selected is TCable)
            {
                var tcable = Selected as TCable;
                try
                {
                    MyGraph.Disconnect(tcable.Dest.Mod,
                                       (tcable.Index > 0 ? tcable.Dest.Mod.Definition.ParamNames[tcable.Index - 1] : null));
                }
                catch (ModelException me)
                {
                    MessageBox.Show(me.Message);
                }
                Dirty = true;
                Invalidate();
            }
            else if (Selected is TModule)
            {
                var tmod = Selected as TModule;
                try
                {
                    MyGraph.RemoveModule(tmod.Mod);
                }
                catch (ModelException me)
                {
                    MessageBox.Show(me.Message);
                }
                Dirty = true;
                Invalidate();
            }
        }

        void AddModule(string name, Point pos)
        {
            try
            {
                var mod = MyGraph.AddModule(name);
                mod.X = pos.X;
                mod.Y = pos.Y;
            }
            catch (ModelException me)
            {
                MessageBox.Show(me.Message);
            }
            Dirty = true;
            Invalidate();
        }

        void DoLayout(Graphics g)
        {
            Things = new List<IThing>();

            foreach (var mod in MyGraph.Modules)
                Things.Add(new TModule(mod, this, g));

            var tmods = Things.ToArray().OfType<TModule>();

            foreach (var tmod in tmods)
            {
                if (tmod.Mod.Definition.OutChannels > 0)
                    Things.Add(new TPin(tmod, true, 0, this));

                if (tmod.Mod.Definition.InChannels > 0)
                    Things.Add(new TPin(tmod, false, 0, this));

                for (int i=0; i<tmod.Mod.Params.Length; i++)
                    Things.Add(new TPin(tmod, false, i+1, this));

                for (int i=0; i<tmod.Mod.Inputs.Length; i++)
                    if (tmod.Mod.Inputs[i] != null)
                        Things.Add(new TCable(tmods.First(tm => tm.Mod == tmod.Mod.Inputs[i]), tmod, i, this));
            }         
        }

        IThing Hit(Point p)
        {
            IThing res = null;
            if (Things != null) foreach (var thing in Things.OrderBy(t => t.Priority))
                if (thing.HitRect.Contains(p) && thing.HitTest2(p))
                    res = thing;
            return res;
        }

        void SetHovered(IThing t)
        {
            if (t != Hovered)
            {
                Hovered = t;
                Invalidate();
            }
        }

        void SetSelected(IThing t)
        {
            if (t != Selected)
            {
                Selected = t;
                Invalidate();
                Dirty = true;
            }
        }

        static void PaintRoundedRect(Graphics g, Rectangle rect, int radius, Brush fill, Pen outline = null)
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


        static Rectangle RectFromPoints(Point[] points, int extend)
        {
            int x0 = Int32.MaxValue, x1 = Int32.MinValue, y0 = Int32.MaxValue, y1 = Int32.MinValue;
            foreach (var p in points)
            {
                x0 = Math.Min(p.X, x0);
                x1 = Math.Max(p.X, x1);
                y0 = Math.Min(p.Y, y0);
                y1 = Math.Max(p.Y, y1);
            }

            return new Rectangle(x0 - extend, y0 - extend, (x1 - x0) + 2 * extend, (y1 - y0) + 2 * extend);
        }


        static int DistanceFromSegment(Point p, Point a, Point b)
        {
            float px = p.X, py = p.Y, ax = a.X, ay = a.Y, bx = b.X, by = b.Y;

            float len = (float)Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
            float d = Math.Abs((bx - ax) * (py - ay) - (by - ay) * (px - ax)) / len;

            float dot = (bx - ax) * (px - bx) + (by - ay) * (py - by);
            if (dot > 0)
                d = (float)Math.Sqrt((px - bx) * (px - bx) + (py - by) * (py - by));
            else
            {
                dot = (bx - ax) * (px - ax) + (by - ay) * (py - ay);
                if (dot < 0)
                    d = (float)Math.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));
            }

            return (int)Math.Round(d);
        }


        MenuItem[] GetModuleMenuItems(string prefix, Action<string> action)
        {
            var names = ModuleDefinition.Registry.Keys.Where(n => !n.StartsWith("!")).ToList();

            var menuitems = new List<MenuItem>();
            var submenus = new Dictionary<string, MenuItem[]>();

            if (prefix != "")
                names = names.Where(n => n.StartsWith(prefix)).Select(n => n.Substring(prefix.Length)).ToList();

            foreach (var name in names)
            {
                var slpos = name.IndexOf('/');
                if (slpos >= 0)
                    submenus[name.Substring(0, slpos)] = GetModuleMenuItems(prefix + name.Substring(0, slpos + 1), action);
                else
                    menuitems.Add(new MenuItem(name, (o, e) => action(prefix + name)));
            }

            menuitems.Sort((a, b) => String.Compare(a.Text, b.Text));

            if (submenus.Count > 0) menuitems.Insert(0, new MenuItem("-"));
            menuitems.InsertRange(0, submenus.OrderBy(s => s.Key).Select(s => new MenuItem(s.Key, s.Value)));

            return menuitems.ToArray();
        }

        enum DragOp
        {
            None,
            Module,
            CableFrom,
            CableTo,
        };

        bool Dirty = true;
        List<IThing> Things;
        IThing Hovered;
        IThing Selected;
        DragOp Drag;
        Point DragOffset;
        TCable DragCable;

        Font ModHeadFnt = new Font(FontFamily.GenericSansSerif, 8);
        Font ModParamFnt = new Font(FontFamily.GenericSansSerif, 7);

        Brush ModBack = new SolidBrush(Color.DarkSlateBlue);
        Brush ModText = new SolidBrush(Color.White);

        Brush ModPinMono   = new SolidBrush(Color.PaleTurquoise);
        Brush ModPinStereo = new SolidBrush(Color.White);

        Pen ModBorder = new Pen(Color.LightSteelBlue,1);

        Pen ModHoveredPen = new Pen(Color.Yellow, 1);
        Pen ModSelectedPen = new Pen(Color.Yellow, 3);
        Brush ModSelectedBrush = new SolidBrush(Color.Yellow);

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