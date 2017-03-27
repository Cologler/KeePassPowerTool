using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeePassPowerTool.Extensions
{
    static class ToolStripItemCollectionExtensions
    {
        public static void AddSeparator(this ToolStripItemCollection collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count > 0 && !(collection[collection.Count - 1] is ToolStripSeparator))
            {
                collection.Add(new ToolStripSeparator());
            }
        }
    }
}
