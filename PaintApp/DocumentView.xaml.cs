using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace PaintApp
{
    public partial class DocumentView : UserControl
    {
        public string CurrentTool { get; set; } = "Pen";
        private WriteableBitmap _bitmap;
        private System.Drawing.Bitmap _backingBitmap;
        private Point _startPoint;
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
            }
        }

        private void CanvasImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(CanvasImage);
                if (CurrentTool == "Pen")
                {
                    DrawLine(_startPoint, currentPoint, System.Drawing.Color.Black, 2);
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
                Point endPoint = e.GetPosition(CanvasImage);
                if (CurrentTool == "Line")
                {
                    DrawLine(_startPoint, endPoint, System.Drawing.Color.Black, 2);
                }
                else if (CurrentTool == "Ellipse")
                {
                    DrawEllipse(_startPoint, endPoint);
                }
                else if (CurrentTool == "Star")
                {
                    DrawStar(_startPoint, endPoint, 5, 0.5);
                }
                else if (CurrentTool == "Fill")
                {
                    FloodFill((int)endPoint.X, (int)endPoint.Y, System.Drawing.Color.Red, System.Drawing.Color.White);
                }
                _isDrawing = false;
            }
        }

        private void DrawLine(Point p1, Point p2, System.Drawing.Color color, int width)
        {
            using (Graphics g = Graphics.FromImage(_backingBitmap))
            {
                g.DrawLine(new System.Drawing.Pen(color, width), (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
            }
            UpdateWpfImage();
        }

        private void DrawEllipse(Point p1, Point p2)
        {
            using (Graphics g = Graphics.FromImage(_backingBitmap))
            {
                float x = Math.Min((float)p1.X, (float)p2.X);
                float y = Math.Min((float)p1.Y, (float)p2.Y);
                float w = Math.Abs((float)(p2.X - p1.X));
                float h = Math.Abs((float)(p2.Y - p1.Y));
                g.DrawEllipse(System.Drawing.Pens.Black, x, y, w, h);
            }
            UpdateWpfImage();
        }

        private void DrawStar(Point center, Point edge, int points, double innerRatio)
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
                g.FillPolygon(System.Drawing.Brushes.Gold, pts);
                g.DrawPolygon(System.Drawing.Pens.Black, pts);
            }
            UpdateWpfImage();
        }

        private void FloodFill(int x, int y, System.Drawing.Color targetColor, System.Drawing.Color replacementColor)
        {
        }

        private void ApplyZoom(double factor)
        {
            CanvasImage.LayoutTransform = new System.Windows.Media.ScaleTransform(factor, factor);
        }
    }
}