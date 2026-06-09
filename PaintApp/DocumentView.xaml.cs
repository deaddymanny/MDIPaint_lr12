using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PaintApp
{
    public partial class DocumentView : UserControl
    {
        public string CurrentTool { get; set; } = "Pen";
        public System.Drawing.Color CurrentColor { get; set; } = System.Drawing.Color.Black;
        public int StarPoints { get; set; } = 5;
        public double StarInnerRatio { get; set; } = 0.5;

        private WriteableBitmap _bitmap;
        private System.Drawing.Bitmap _backingBitmap;
        private System.Windows.Point _startPoint;
        private bool _isDrawing;

        public DocumentView()
        {
            InitializeComponent();
            _backingBitmap = new System.Drawing.Bitmap(800, 600, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(_backingBitmap))
            {
                g.Clear(System.Drawing.Color.White);
            }
            UpdateWpfImage();
        }

        public async Task LoadImageAsync(string path)
        {
            using (var bmp = new System.Drawing.Bitmap(path))
            {
                _backingBitmap = new System.Drawing.Bitmap(bmp);
            }
            UpdateWpfImage();
        }

        public async Task SaveImageAsync(string path)
        {
            using (var bmp = GetBitmap())
            {
                string ext = Path.GetExtension(path).ToLower();
                ImageFormat format = ext == ".jpg" || ext == ".jpeg" ? ImageFormat.Jpeg :
                                     ext == ".png" ? ImageFormat.Png : ImageFormat.Bmp;
                bmp.Save(path, format);
            }
        }

        public System.Drawing.Bitmap GetBitmap()
        {
            return new System.Drawing.Bitmap(_backingBitmap);
        }

        public void SetBitmap(System.Drawing.Bitmap bmp)
        {
            _backingBitmap = bmp;
            UpdateWpfImage();
        }

        private void UpdateWpfImage()
        {
            Rectangle rect = new Rectangle(0, 0, _backingBitmap.Width, _backingBitmap.Height);
            BitmapData bmpData = _backingBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int bytes = Math.Abs(bmpData.Stride) * _backingBitmap.Height;
            byte[] rgbValues = new byte[bytes];
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);
            _backingBitmap.UnlockBits(bmpData);

            _bitmap = new WriteableBitmap(_backingBitmap.Width, _backingBitmap.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
            _bitmap.WritePixels(new Int32Rect(0, 0, _backingBitmap.Width, _backingBitmap.Height), rgbValues, bmpData.Stride, 0);
            CanvasImage.Source = _bitmap;
        }

        private void CanvasImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDrawing = true;
                _startPoint = e.GetPosition(CanvasImage);
                if (CurrentTool == "ZoomIn" || CurrentTool == "ZoomOut")
                {
                    ApplyZoom(CurrentTool == "ZoomIn" ? 1.2 : 0.8);
                    _isDrawing = false;
                }
                else if (CurrentTool == "Fill")
                {
                    System.Drawing.Point pt = new System.Drawing.Point((int)_startPoint.X, (int)_startPoint.Y);
                    System.Drawing.Color targetColor = _backingBitmap.GetPixel(pt.X, pt.Y);
                    FloodFill(pt.X, pt.Y, targetColor, CurrentColor);
                    _isDrawing = false;
                }
            }
        }

        private void CanvasImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point currentPoint = e.GetPosition(CanvasImage);
                if (CurrentTool == "Pen")
                {
                    DrawLine(_startPoint, currentPoint, CurrentColor, 2);
                    _startPoint = currentPoint;
                }
                else if (CurrentTool == "Eraser")
                {
                    DrawLine(_startPoint, currentPoint, System.Drawing.Color.White, 15);
                    _startPoint = currentPoint;
                }
            }
        }

        private void CanvasImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                System.Windows.Point endPoint = e.GetPosition(CanvasImage);
                if (CurrentTool == "Line")
                {
                    DrawLine(_startPoint, endPoint, CurrentColor, 2);
                }
                else if (CurrentTool == "Ellipse")
                {
                    DrawEllipse(_startPoint, endPoint, CurrentColor);
                }
                else if (CurrentTool == "Star")
                {
                    DrawStar(_startPoint, endPoint, StarPoints, StarInnerRatio, CurrentColor);
                }
                _isDrawing = false;
            }
        }

        private void DrawLine(System.Windows.Point p1, System.Windows.Point p2, System.Drawing.Color color, int width)
        {
            using (Graphics g = Graphics.FromImage(_backingBitmap))
            {
                g.DrawLine(new System.Drawing.Pen(color, width), (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
            }
            UpdateWpfImage();
        }

        private void DrawEllipse(System.Windows.Point p1, System.Windows.Point p2, System.Drawing.Color color)
        {
            using (Graphics g = Graphics.FromImage(_backingBitmap))
            {
                float x = Math.Min((float)p1.X, (float)p2.X);
                float y = Math.Min((float)p1.Y, (float)p2.Y);
                float w = Math.Abs((float)(p2.X - p1.X));
                float h = Math.Abs((float)(p2.Y - p1.Y));

                using (SolidBrush brush = new SolidBrush(color))
                using (Pen pen = new Pen(color, 2))
                {
                    g.FillEllipse(brush, x, y, w, h);
                    g.DrawEllipse(pen, x, y, w, h);
                }
            }
            UpdateWpfImage();
        }

        private void DrawStar(System.Windows.Point center, System.Windows.Point edge, int points, double innerRatio, System.Drawing.Color color)
        {
            using (Graphics g = Graphics.FromImage(_backingBitmap))
            {
                double outerRadius = Math.Sqrt(Math.Pow(edge.X - center.X, 2) + Math.Pow(edge.Y - center.Y, 2));
                double innerRadius = outerRadius * innerRatio;
                System.Drawing.PointF[] pts = new System.Drawing.PointF[points * 2];
                double angle = -Math.PI / 2;
                double step = Math.PI / points;

                for (int i = 0; i < points * 2; i++)
                {
                    double r = i % 2 == 0 ? outerRadius : innerRadius;
                    pts[i].X = (float)(center.X + r * Math.Cos(angle));
                    pts[i].Y = (float)(center.Y + r * Math.Sin(angle));
                    angle += step;
                }

                using (SolidBrush brush = new SolidBrush(color))
                using (Pen pen = new Pen(System.Drawing.Color.Black, 1))
                {
                    g.FillPolygon(brush, pts);
                    g.DrawPolygon(pen, pts);
                }
            }
            UpdateWpfImage();
        }

        private void FloodFill(int x, int y, System.Drawing.Color targetColor, System.Drawing.Color replacementColor)
        {
            if (targetColor == replacementColor) return;

            Stack<System.Drawing.Point> stack = new Stack<System.Drawing.Point>();
            stack.Push(new System.Drawing.Point(x, y));

            int width = _backingBitmap.Width;
            int height = _backingBitmap.Height;

            while (stack.Count > 0)
            {
                System.Drawing.Point pt = stack.Pop();

                if (pt.X < 0 || pt.X >= width || pt.Y < 0 || pt.Y >= height) continue;

                if (_backingBitmap.GetPixel(pt.X, pt.Y) == targetColor)
                {
                    _backingBitmap.SetPixel(pt.X, pt.Y, replacementColor);

                    stack.Push(new System.Drawing.Point(pt.X + 1, pt.Y));
                    stack.Push(new System.Drawing.Point(pt.X - 1, pt.Y));
                    stack.Push(new System.Drawing.Point(pt.X, pt.Y + 1));
                    stack.Push(new System.Drawing.Point(pt.X, pt.Y - 1));
                }
            }
            UpdateWpfImage();
        }

        private void ApplyZoom(double factor)
        {
            CanvasImage.LayoutTransform = new System.Windows.Media.ScaleTransform(factor, factor);
        }
    }
}