using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CP
{
    public class ColorPicker : Form
    {
        //#==================================================================== CONSTANTS
        private const int COLOR_COUNT = 360;

        //#==================================================================== ENUM
        private enum ControlState
        {
            None, Donut, Triangle, AlphaRect
        }
        
        //#==================================================================== CONTROLS
        private NumericUpDown _numHue = new NumericUpDown();
        private NumericUpDown _numSaturation = new NumericUpDown();
        private NumericUpDown _numBrightness = new NumericUpDown();
        private NumericUpDown _numRed = new NumericUpDown();
        private NumericUpDown _numGreen = new NumericUpDown();
        private NumericUpDown _numBlue = new NumericUpDown();
        private NumericUpDown _numAlpha = new NumericUpDown();
        private Button _btnOK = new Button();
        private Button _btnDefault = new Button();
        private Button _btnOld = new Button();

        //#==================================================================== VARIABLES
        private Bitmap _colorWheel;
        private Bitmap _alphaBackground;
        private ControlState _controlState = ControlState.None;
        private Color _oldColor, _defaultColor;
        private bool _isManualChange = false; // set to true when manually changing numericupdown values
        private Rectangle _oldColorRect, _defaultColorRect;
        private Brush _alphaPatternBrush;

        private SizeF _textSizePattern;
        
        //#==================================================================== EVENTS
        public event EventHandler<EventArgs> SelectedColorChanged;

        //#==================================================================== INITIALIZE
        public ColorPicker() : this(Color.White)
        {
        }
        private SizeF GetTextSize(Graphics g)
        {
            var font = new Font(SystemFonts.DefaultFont, FontStyle.Regular);
            var drawFormat = new System.Drawing.StringFormat(StringFormatFlags.NoClip | StringFormatFlags.NoWrap);
            var size = g.MeasureString("H", font, new PointF(0, 0), drawFormat);

            return size;
        }

        public ColorPicker(Color defaultColor)
        {
            using (Graphics g = this.CreateGraphics())
            {
                _textSizePattern = GetTextSize(g);
            }
            this.ClientSize = new Size((int)(23f* _textSizePattern.Width), (int)(32f* _textSizePattern.Height));
            
            _numHue.DecimalPlaces = _numSaturation.DecimalPlaces = _numBrightness.DecimalPlaces = 2;
            _numHue.Maximum = 359.99m;
            _numHue.Location = new Point(3 * X, Y + AlphaYPos + Thickness);
            _numHue.Width = _numSaturation.Width = _numBrightness.Width = _numRed.Width = _numGreen.Width = _numBlue.Width = _numAlpha.Width = (int)(4.5f*_textSizePattern.Width);
            _numHue.Height = _numSaturation.Height = _numBrightness.Height = _numRed.Height = _numGreen.Height = _numBlue.Height = _numAlpha.Height = (int)(1.2f * _textSizePattern.Height);
            _numHue.ValueChanged += numHSB_ValueChanged;
            _numSaturation.Location = new Point(_numHue.Left, _numHue.Bottom + Y);
            _numSaturation.ValueChanged += numHSB_ValueChanged;
            _numBrightness.Location = new Point(_numHue.Left, _numSaturation.Bottom + Y);
            _numBrightness.ValueChanged += numHSB_ValueChanged;
            _numRed.Maximum = _numGreen.Maximum = _numBlue.Maximum = _numAlpha.Maximum = 255;
            _numRed.Location = new Point(_numHue.Right + 3*X, Y + AlphaYPos + Thickness);
            _numRed.ValueChanged += numRGB_ValueChanged;
            _numGreen.Location = new Point(_numSaturation.Right + 3 * X, _numRed.Bottom + Y);
            _numGreen.ValueChanged += numRGB_ValueChanged;
            _numBlue.Location = new Point(_numBrightness.Right + 3 * X, _numGreen.Bottom + Y);
            _numBlue.ValueChanged += numRGB_ValueChanged;
            _numAlpha.Location = new Point(_numBrightness.Right + 3 * X, _numBlue.Bottom + Y);
            _numAlpha.ValueChanged += numRGB_ValueChanged;
            _btnOK.Location = new Point(_numRed.Right + X, Y + AlphaYPos + Thickness);
            _btnOK.Text = "OK";
            _btnOK.Width = ClientSize.Width - _numRed.Right - X - X;
            _btnOK.Click += btnOK_Click;

            _btnOld.Location = new Point(_numGreen.Right + X, _numGreen.Top);
            _btnOld.Text = "Old";
            _btnOld.Width = ClientSize.Width - _numRed.Right - X - X;
            _btnOld.Click += (_, __) =>
            {
                SelectedColorAlpha = _oldColor;
                TriggerColorChangedEvent();
            };
            _btnDefault.Location = new Point(_numBlue.Right + X, _numBlue.Top);
            _btnDefault.Text = "Default";
            _btnDefault.Width = ClientSize.Width - _numRed.Right - X - X;
            _btnDefault.Click += (_, __) =>
            {
                SelectedColorAlpha = _defaultColor;
                TriggerColorChangedEvent();
            };
            _btnOK.Height = _btnOld.Height = _btnDefault.Height = _numRed.Height;

            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Color Picker";
            this.Controls.Add(_numHue);
            this.Controls.Add(_numSaturation);
            this.Controls.Add(_numBrightness);
            this.Controls.Add(_numRed);
            this.Controls.Add(_numGreen);
            this.Controls.Add(_numBlue);
            this.Controls.Add(_numAlpha);
            
            this.Controls.Add(_btnOK);
            this.Controls.Add(_btnOld);
            this.Controls.Add(_btnDefault);

            SelectedColor = _oldColor = defaultColor;
            InitializeColorWheel();
            InitializeAlphaPetternBrush();
            InitializeAlphaBackground();

            SelectedColorChanged += (_, __) => InitializeAlphaBackground();
        }
        public void InitializeColorWheel()
        {
            _colorWheel = new Bitmap(Diameter, Diameter);
            Point center = new Point(Radius, Radius);
            using (Graphics g = Graphics.FromImage(_colorWheel))
            using (PathGradientBrush brush = new PathGradientBrush(GetDonutPoints(Radius + 1, center)))
            using (SolidBrush backBrush = new SolidBrush(this.BackColor))
            {
                brush.CenterPoint = center;
                brush.CenterColor = Color.White;
                brush.SurroundColors = GetDonutColors();

                // drawing the donut
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(brush, 0, 0, Radius * 2 - 1, Radius * 2 - 1);
                g.FillEllipse(backBrush, Thickness - 1, Thickness - 1, Diameter - Thickness * 2 + 1, Diameter - Thickness * 2 + 1);
            }
        }

        private void InitializeAlphaPetternBrush()
        {
            var pattern = new Bitmap(2*X, 2*X);
            using (var gp = Graphics.FromImage(pattern))
            {
                gp.FillRectangle(Brushes.White, new Rectangle(0, 0, X, X));
                gp.FillRectangle(Brushes.LightGray, new Rectangle(X, 0, X, X));
                gp.FillRectangle(Brushes.LightGray, new Rectangle(0, X, X, X));
                gp.FillRectangle(Brushes.White, new Rectangle(X, X, X, X));
            }
            _alphaPatternBrush = new TextureBrush(pattern);
        }

        private void InitializeAlphaBackground()
        {
            if (_alphaBackground != null)
                _alphaBackground.Dispose();
            _alphaBackground = new Bitmap(AlphaWidth, AlphaHeight);

            using (var gr = Graphics.FromImage(_alphaBackground))
            using (LinearGradientBrush brush = new LinearGradientBrush(
                                                                    new Point(0, 0),
                                                                    new Point(AlphaWidth, AlphaHeight),
                                                                    Color.FromArgb(0, SelectedColor),
                                                                    Color.FromArgb(255, SelectedColor)))
            {
                gr.SmoothingMode = SmoothingMode.AntiAlias;
                                
                gr.FillRectangle(_alphaPatternBrush, 0, 0, Diameter, Thickness);
                gr.FillRectangle(brush, 0, 0, AlphaWidth, AlphaHeight);
            }
        }


        //#==================================================================== FINALIZING
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _colorWheel.Dispose();
            _alphaBackground.Dispose();
            _alphaPatternBrush.Dispose();
        }
        //#==================================================================== FUNCTIONS
        private Point[] GetDonutPoints(int radius, Point center)
        {
            Point[] points = new Point[COLOR_COUNT];
            for (int degree = 0; degree < points.Length; degree++)
                points[degree] = GetPoint(radius, center, degree);
            return points;
        }
        private Point GetPoint(int radius, Point center, int degree)
        {
            float radians = (float)((degree * Math.PI) / 180);
            int x = (int)(center.X + radius * Math.Cos(radians));
            int y = (int)(center.Y + radius * Math.Sin(radians));
            return new Point(x, y);
        }
        private Color[] GetDonutColors()
        {
            Color[] colors = new Color[COLOR_COUNT];
            for (int degree = 0; degree < colors.Length; degree++)
                colors[degree] = new ColorEx.HSB(degree, 100, 100).ToColor();
            return colors;
        }
        private bool IsInsideDonut(Point pt)
        {
            float distance = (float)Math.Sqrt(Math.Pow(pt.Y - Center.Y, 2) + Math.Pow(pt.X - Center.X, 2));
            return (distance > InnerRadius && distance < Radius);
        }
        
        private bool IsInsideRect(Point loc, Rectangle rect)
        {
            return (rect.Top <= loc.Y && rect.Top + rect.Height >= loc.Y) &&
                  (rect.Left <= loc.X && rect.Left + rect.Width >= loc.X);
        }

        private bool IsInsideTriangle(Point pt)
        {
            if (!(pt.Y > TriangleY && pt.Y < TriangleY + TriangleHeight))
                return false; // check vertical point
            float yPercent = (pt.Y - TriangleY) / (float)TriangleHeight;
            float length = TriangleWidth * yPercent;
            return (pt.X > Center.X - length / 2 && pt.X < Center.X + length / 2); // check horizontal point
        }

        private bool IsInsideAlpha(Point pt)
        {
            return (pt.X > X && pt.X < AlphaWidth) &&
               (pt.Y > AlphaYPos && pt.Y < AlphaYPos + AlphaHeight);
        }

        private float GetHue(Point pt)
        {
            float radian = (float)Math.Atan2(pt.Y - Center.Y, pt.X - Center.X);
            float degree = (float)((radian * 180) / Math.PI);
            return (degree < 0 ? degree + 360 : degree);
        }
        private float GetSaturation(Point pt)
        {
            float yPercent = (pt.Y - TriangleY) / (float)TriangleHeight;
            float length = TriangleWidth * yPercent;
            return (pt.X - Center.X + length / 2) / length * 100;
        }
        private float GetBrightness(Point pt)
        {
            return (pt.Y - TriangleY) / (float)TriangleHeight * 100;
        }
        private int GetAlpha(Point pt)
        {
            var alpha =  (pt.X - X) / (float)AlphaWidth * 255f;
            return (int)alpha;
        }

        //#==================================================================== PROPERTIES
        private int X
        {
            get { return (int)_textSizePattern.Height; }
        }
        private int Y
        {
            get { return (int)_textSizePattern.Height; }
        }
        private int AlphaWidth
        { 
            get { return Diameter; } 
        }
        private int AlphaHeight
        {
            get { return Thickness; }
        }
        private int AlphaYPos
        {
            get { return Y+Diameter+Y; }
        }
        private int Diameter
        {
            get { return ClientSize.Width-2*X; }
        }
        private int Radius
        {
            get { return Diameter / 2; }
        }
        private int InnerRadius
        {
            get { return Radius - Thickness; }
        }
        private int Thickness
        {
            get { return 25; }
        }
        private Point Center
        {
            get { return new Point(X + Radius, Y + Radius); }
        }

        private int TriangleY
        {
            get { return Center.Y - InnerRadius; }
        }
        private int TriangleHeight
        {
            get { return (InnerRadius * 3) / 2; }
        }
        private float TriangleWidth
        {
            get { return (TriangleHeight * 2) / (float)Math.Sqrt(3); }
        }

        private Rectangle HuePickerRect
        {
            get
            {
                int radius = (int)(Y/2);
                Point pt = GetPoint(InnerRadius + Thickness / 2, Center, (int)Hue);
                return new Rectangle(pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
            }
        }
        private Rectangle SBPickerRect
        {
            get
            {
                float xPercent = Saturation / 100;
                float yPercent = Brightness / 100;
                float length = TriangleWidth * yPercent;
                int radius = (int)(Y / 2);
                int x = (int)(Center.X - length / 2 + length * xPercent);
                int y = TriangleY + (int)(yPercent * TriangleHeight);
                Point pt = new Point(x, y);
                return new Rectangle(pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
            }
        }

        private Rectangle AlphaPickerRect
        {
            get
            {
                int radius = (int)(Y / 2);
                int width_pos = (int)(Alpha/255f*AlphaWidth);
                return new Rectangle(X+width_pos-radius, AlphaYPos+ AlphaHeight/2-radius, radius*2, radius*2);
            }
        }

        public Color SelectedColor
        {
            get { return new ColorEx.HSB(Hue, Saturation, Brightness).ToColor(); }
            set
            {
                Red = value.R;
                Green = value.G;
                Blue = value.B;
                Alpha = 255;
            }
        }

        public Color SelectedColorAlpha
        {
            get { return Color.FromArgb(Alpha, SelectedColor); }
            set
            {
                Red = value.R;
                Green = value.G;
                Blue = value.B;
                Alpha = value.A;
            }
        }

        public Color OldColor
        {
            get { return this._oldColor; }
            set { this._oldColor = value; }
        }
        public Color DefaultColor
        {
            get { return this._defaultColor; }
            set { this._defaultColor = value; }
        }
        private float Hue
        {
            get { return (float)_numHue.Value; }
            set
            {
                if (float.IsNaN(value))
                    return;
                _numHue.Value = (decimal)Math.Round(value, 2);
            }
        }
        private float Saturation
        {
            get { return (float)_numSaturation.Value; }
            set
            {
                if (float.IsNaN(value))
                    return;
                _numSaturation.Value = (decimal)(Math.Min(Math.Max(value, 0), 100));
            }
        }
        private float Brightness
        {
            get { return (float)_numBrightness.Value; }
            set
            {
                if (float.IsNaN(value))
                    return;
                _numBrightness.Value = (decimal)Math.Min(Math.Max(value, 0), 100);
            }
        }
        private int Red
        {
            get { return (int)_numRed.Value; }
            set { _numRed.Value = (decimal)value; }
        }
        private int Green
        {
            get { return (int)_numGreen.Value; }
            set { _numGreen.Value = (decimal)value; }
        }
        private int Blue
        {
            get { return (int)_numBlue.Value; }
            set { _numBlue.Value = (decimal)value; }
        }
        private int Alpha
        {
            get { return (int)_numAlpha.Value; }
            set { _numAlpha.Value = (decimal)Math.Min(Math.Max(value,0), 255) ; }
        }

        //#==================================================================== EVENTS
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawAlphaDonut(e.Graphics);
            DrawDonut(e.Graphics);
            DrawTriangle(e.Graphics);
            //DrawColorSquare(e.Graphics);
            DrawHuePicker(e.Graphics);
            DrawSBPicker(e.Graphics);
            DrawAlphaPicker(e.Graphics);
            DrawLabels(e.Graphics);
            base.OnPaint(e);
        }
        private void DrawAlphaDonut(Graphics g)
        {
            g.DrawImage(_alphaBackground, X, AlphaYPos);
        }
        private void DrawDonut(Graphics g)
        {
            g.DrawImage(_colorWheel, X, Y);
        }
        private void DrawTriangle(Graphics g)
        {
            // iterate top to bottom
            for (int y = TriangleY; y < TriangleY + TriangleHeight; y++)
            {
                float yPercent = (y - TriangleY) / (float)TriangleHeight;
                float length = TriangleWidth * yPercent;
                PointF pt1 = new PointF(Center.X - length / 2 - 1, y);
                PointF pt2 = new PointF(Center.X + length / 2 + 1, y);
                Color col1 = new ColorEx.HSB(Hue, 0, yPercent * 100).ToColor();
                Color col2 = new ColorEx.HSB(Hue, 100, yPercent * 100).ToColor();
                using (LinearGradientBrush brush = new LinearGradientBrush(pt1, pt2, col1, col2))
                using (Pen pen = new Pen(brush))
                    g.DrawLine(pen, Center.X - length / 2, y, Center.X + length / 2, y);
            }
        }
        private void DrawColorSquare(Graphics g)
        {
            int height = (_numBlue.Bottom - _numGreen.Top) / 2;
            using (SolidBrush brush = new SolidBrush(SelectedColorAlpha))
            {
                var rect = new Rectangle(_btnOK.Left, _numGreen.Top, _btnOK.Width, height);
                g.FillRectangle(_alphaPatternBrush, rect);
                g.FillRectangle(brush, rect);
            }

            using (SolidBrush brush = new SolidBrush(OldColor))
            {
                _oldColorRect = new Rectangle(_btnOK.Left, _numBlue.Top, _btnOK.Width, height);
                g.FillRectangle(_alphaPatternBrush, _oldColorRect);
                g.FillRectangle(brush, _oldColorRect);

                TextRenderer.DrawText(g, "Old", this.Font, _oldColorRect, SystemColors.WindowText, TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

            using (SolidBrush brush = new SolidBrush(DefaultColor))
            {
                _defaultColorRect = new Rectangle(_btnOK.Left, _numBlue.Top + height, _btnOK.Width, height);
                g.FillRectangle(_alphaPatternBrush, _defaultColorRect);
                g.FillRectangle(brush, _defaultColorRect);

                TextRenderer.DrawText(g, "Default", this.Font, _defaultColorRect, SystemColors.WindowText, TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }

        }
        private void DrawHuePicker(Graphics g)
        {
            using (Pen pen = new Pen(Color.Black, 2))
                g.DrawEllipse(pen, HuePickerRect);
        }
        private void DrawSBPicker(Graphics g)
        {
            int brightness = (Brightness > 50 ? 0 : 100);
            using (Pen pen = new Pen(new ColorEx.HSB(0, 0, brightness).ToColor(), 2))
                g.DrawEllipse(pen, SBPickerRect);
        }
        private void DrawAlphaPicker(Graphics g)
        {
            int brightness = (Brightness > 50 ? 0 : 100);
            using (Pen pen = new Pen(new ColorEx.HSB(0, 0, brightness).ToColor(), 2))
                g.DrawEllipse(pen, AlphaPickerRect);
        }
        private void DrawLabels(Graphics g)
        {
            string[] labels = new string[] { "H", "S", "B", "R", "G", "B" };
            Rectangle rect = new Rectangle(X, _numHue.Top - 2, 1, _numHue.Height);
            for (int i = 0; i < 3; i++)
            {
                TextRenderer.DrawText(g, labels[i], this.Font, rect, SystemColors.WindowText, TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
                rect.X = _numHue.Right + X;
                TextRenderer.DrawText(g, labels[i + 3], this.Font, rect, SystemColors.WindowText, TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
                rect.X = X;
                rect.Y += _numHue.Height + Y;
            }
            rect.X = _numHue.Right + X;
            TextRenderer.DrawText(g, "A", this.Font, rect, SystemColors.WindowText, TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {

            if (IsInsideDonut(e.Location))
            {
                _controlState = ControlState.Donut;
                Hue = GetHue(e.Location);
                TriggerColorChangedEvent();
            }
            else if (IsInsideTriangle(e.Location))
            {
                _controlState = ControlState.Triangle;
                Saturation = GetSaturation(e.Location);
                Brightness = GetBrightness(e.Location);
                TriggerColorChangedEvent();
            }
            else if (IsInsideAlpha(e.Location))
            {
                _controlState = ControlState.AlphaRect;
                Alpha = GetAlpha(e.Location);
                TriggerColorChangedEvent();
            }
            //else if (IsInsideRect(e.Location, _oldColorRect))
            //{
            //    SelectedColorAlpha = _oldColor;
            //    TriggerColorChangedEvent();
            //}
            //else if (IsInsideRect(e.Location, _defaultColorRect))
            //{
            //    SelectedColorAlpha = _defaultColor;
            //    TriggerColorChangedEvent();
            //}
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_controlState == ControlState.Donut)
            {
                Hue = GetHue(e.Location);
                TriggerColorChangedEvent();
            }
            else if (_controlState == ControlState.Triangle)
            {
                Saturation = GetSaturation(e.Location);
                Brightness = GetBrightness(e.Location);
                TriggerColorChangedEvent();
            }
            else if (_controlState == ControlState.AlphaRect)
            {
                Alpha = GetAlpha(e.Location);
                TriggerColorChangedEvent();
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _controlState = ControlState.None;
            base.OnMouseUp(e);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
                this.Close();
            base.OnKeyPress(e);
        }

        private void numHSB_ValueChanged(object sender, EventArgs e)
        {
            if (_isManualChange == false)
            {
                _isManualChange = true;
                ColorEx.RGB col = new ColorEx.HSB(Hue, Saturation, Brightness).ToRGB();
                _numRed.Value = col.Red;
                _numGreen.Value = col.Green;
                _numBlue.Value = col.Blue;
                this.Invalidate(false);
                _isManualChange = false;
            }
            else
            {
                TriggerColorChangedEvent();
            }
        }
        private void numRGB_ValueChanged(object sender, EventArgs e)
        {
            if (_isManualChange == false)
            {
                _isManualChange = true;
                ColorEx.HSB col = new ColorEx.RGB(Red, Green, Blue).ToHSB();
                _numHue.Value = (decimal)col.Hue;
                _numSaturation.Value = (decimal)col.Saturation;
                _numBrightness.Value = (decimal)col.Brightness;
                this.Invalidate(false);
                _isManualChange = false;
            }
            else
            {
                TriggerColorChangedEvent();
            }
        }

        private void TriggerColorChangedEvent()
        {
            if (SelectedColorChanged != null)
                SelectedColorChanged(this, EventArgs.Empty);
        }
    }
}
