using System;
using System.Drawing;
using System.Threading;
using PluginInterface;

namespace PluginLocation
{
    public class Location : IPlugin
    {
        public string Name => "Дата и геолокация";
        public string Author => "Катаргин Р.А.";

        public void TransformAsync(Bitmap bitmap, IProgress<int> progress, CancellationToken cancellationToken)
        {
            progress.Report(10);
            if (cancellationToken.IsCancellationRequested) return;

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                progress.Report(30);
                if (cancellationToken.IsCancellationRequested) return;

                Font font = new Font("Arial", 14, FontStyle.Bold);
                Brush brush = Brushes.Yellow;

                string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string geo = "Geo: 00.0° N, 00.0° E";

                SizeF dateSize = g.MeasureString(date, font);
                SizeF geoSize = g.MeasureString(geo, font);

                float x = bitmap.Width - Math.Max(dateSize.Width, geoSize.Width) - 10;
                float y = bitmap.Height - dateSize.Height - geoSize.Height - 10;

                progress.Report(60);
                if (cancellationToken.IsCancellationRequested) return;

                g.DrawString(date, font, brush, x, y);
                g.DrawString(geo, font, brush, x, y + dateSize.Height);

                progress.Report(100);
            }
        }
    }
}
