using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using PluginInterface;

namespace PluginGray
{
    public class Gray : IPlugin
    {
        public string Name => "Оттенки серого";
        public string Author => "Student";

        public void TransformAsync(Bitmap bitmap, IProgress<int> progress, CancellationToken cancellationToken)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            byte[] rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);

            for (int y = 0; y < bitmap.Height; y++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                for (int x = 0; x < bitmap.Width; x++)
                {
                    int index = y * bmpData.Stride + x * 3;
                    byte gray = (byte)(rgbValues[index] * 0.114 + rgbValues[index + 1] * 0.587 + rgbValues[index + 2] * 0.299);
                    rgbValues[index] = rgbValues[index + 1] = rgbValues[index + 2] = gray;
                }
                progress.Report((y * 100) / bitmap.Height);
            }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, bmpData.Scan0, bytes);
            bitmap.UnlockBits(bmpData);
        }
    }
}
