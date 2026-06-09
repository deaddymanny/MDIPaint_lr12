using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using PluginInterface;

namespace PluginMedian
{
    public class Median : IPlugin
    {
        public string Name => "Медианный фильтр";
        public string Author => "Катаргин Р.А.";

        public void TransformAsync(Bitmap bitmap, IProgress<int> progress, CancellationToken cancellationToken)
        {
            Bitmap temp = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);

            for (int y = 1; y < bitmap.Height - 1; y++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                for (int x = 1; x < bitmap.Width - 1; x++)
                {
                    byte[] r = new byte[9];
                    byte[] g = new byte[9];
                    byte[] b = new byte[9];

                    int k = 0;
                    for (int fy = -1; fy <= 1; fy++)
                    {
                        for (int fx = -1; fx <= 1; fx++)
                        {
                            Color c = bitmap.GetPixel(x + fx, y + fy);
                            r[k] = c.R;
                            g[k] = c.G;
                            b[k] = c.B;
                            k++;
                        }
                    }

                    Array.Sort(r);
                    Array.Sort(g);
                    Array.Sort(b);

                    temp.SetPixel(x, y, Color.FromArgb(r[4], g[4], b[4]));
                }
                progress.Report((y * 100) / bitmap.Height);
            }

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(temp, 0, 0);
            }
            temp.Dispose();
        }
    }
}
