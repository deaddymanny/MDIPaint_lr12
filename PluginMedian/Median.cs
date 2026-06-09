using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using PluginInterface;

namespace PluginMedian
{
    public class Median : IPlugin
    {
        public string Name => "Медианный фильтр";
        public string Author => "Катаргин Р.А.";

        private const int KernelSize = 10;
        private const int HalfKernel = KernelSize / 2;
        private const int KernelArea = KernelSize * KernelSize;
        private const int MedianIdx1 = KernelArea / 2 - 1;
        private const int MedianIdx2 = KernelArea / 2;

        public void TransformAsync(Bitmap bitmap, IProgress<int> progress, CancellationToken cancellationToken)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int bytes = Math.Abs(bmpData.Stride) * height;
            byte[] pixels = new byte[bytes];
            Marshal.Copy(bmpData.Scan0, pixels, 0, bytes);
            bitmap.UnlockBits(bmpData);

            int stride = bmpData.Stride;

            int extendedWidth = width + 2 * HalfKernel;
            int extendedHeight = height + 2 * HalfKernel;
            byte[] extendedPixels = new byte[extendedWidth * extendedHeight * 3];

            for (int y = 0; y < extendedHeight; y++)
            {
                for (int x = 0; x < extendedWidth; x++)
                {
                    int srcX = Math.Max(0, Math.Min(x - HalfKernel, width - 1));
                    int srcY = Math.Max(0, Math.Min(y - HalfKernel, height - 1));

                    int srcIdx = srcY * stride + srcX * 3;
                    int dstIdx = (y * extendedWidth + x) * 3;

                    extendedPixels[dstIdx] = pixels[srcIdx];
                    extendedPixels[dstIdx + 1] = pixels[srcIdx + 1];
                    extendedPixels[dstIdx + 2] = pixels[srcIdx + 2];
                }
            }

            byte[] resultPixels = new byte[bytes];
            byte[] r = new byte[KernelArea];
            byte[] g = new byte[KernelArea];
            byte[] b = new byte[KernelArea];

            for (int y = 0; y < height; y++)
            {
                if (cancellationToken.IsCancellationRequested) return;

                for (int x = 0; x < width; x++)
                {
                    int idx = 0;
                    for (int ky = 0; ky < KernelSize; ky++)
                    {
                        for (int kx = 0; kx < KernelSize; kx++)
                        {
                            int extX = x + kx;
                            int extY = y + ky;
                            int extIdx = (extY * extendedWidth + extX) * 3;

                            r[idx] = extendedPixels[extIdx];
                            g[idx] = extendedPixels[extIdx + 1];
                            b[idx] = extendedPixels[extIdx + 2];
                            idx++;
                        }
                    }

                    Array.Sort(r);
                    Array.Sort(g);
                    Array.Sort(b);

                    int resultIdx = y * stride + x * 3;
                    resultPixels[resultIdx] = (byte)((r[MedianIdx1] + r[MedianIdx2]) / 2);
                    resultPixels[resultIdx + 1] = (byte)((g[MedianIdx1] + g[MedianIdx2]) / 2);
                    resultPixels[resultIdx + 2] = (byte)((b[MedianIdx1] + b[MedianIdx2]) / 2);
                }

                progress.Report((int)((y * 100.0) / height));
            }

            bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            Marshal.Copy(resultPixels, 0, bmpData.Scan0, bytes);
            bitmap.UnlockBits(bmpData);

            progress.Report(100);
        }
    }
}
