using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaintApp
{
    public class PluginConfig
    {
        public List<PluginEntry> Plugins { get; set; } = new List<PluginEntry>();
    }

    public class PluginEntry
    {
        public string FileName { get; set; }
        public bool IsEnabled { get; set; }
    }
}
