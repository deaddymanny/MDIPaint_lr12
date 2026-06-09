using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginInterface
{
    public interface IPlugin
    {
        string Name { get; }
        string Author { get; }
        void TransformAsync(Bitmap bitmap, IProgress<int> progress, CancellationToken cancellationToken);
    }
}
