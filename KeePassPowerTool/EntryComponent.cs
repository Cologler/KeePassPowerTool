using KeePassLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeePassPowerTool
{
    abstract class EntryComponent
    {
        public IPluginRoot Root { get; }

        public EntryComponent(IPluginRoot root)
        {
            this.Root = root;
        }

        public abstract IEnumerable<ToolStripMenuItem> BuildMenuItemForToolsMenu();

        public abstract IEnumerable<ToolStripMenuItem> BuildMenuItemForGroupContextMenu();

        public abstract IEnumerable<ToolStripMenuItem> BuildMenuItemForEntryContextMenu();

        protected void OnExecuteAll(object sender, EventArgs e)
        {
            if (!this.Root.Host.Database.IsOpen) return;

            this.Execute(this.Root.Host.Database.RootGroup.GetEntries(true).ToArray());
        }

        protected void OnExecuteGroup(object sender, EventArgs e)
        {
            if (!this.Root.Host.Database.IsOpen) return;

            var g = this.Root.Host.MainWindow.GetSelectedGroup();
            if (g == null) return;
            this.Execute(g.Entries.ToArray());
        }

        protected void OnExecuteSelected(object sender, EventArgs e)
        {
            if (!this.Root.Host.Database.IsOpen) return;

            this.Execute(this.Root.Host.MainWindow.GetSelectedEntries());
        }

        protected abstract void Execute(PwEntry[] entries);
    }
}
