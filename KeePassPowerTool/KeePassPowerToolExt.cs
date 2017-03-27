using KeePass.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeePassPowerTool.Extensions;
using System.Diagnostics;
using System.Windows.Forms;
using KeePassPowerTool.Favicon;

namespace KeePassPowerTool
{
    public class KeePassPowerToolExt : Plugin, IPluginRoot
    {
        private ToolStripMenuItem ToolsMenuRoot;
        private ToolStripMenuItem GroupContextMenuRoot;
        private ToolStripMenuItem EntryContextMenuRoot;

        public IPluginHost Host { get; private set; }

        public bool IsTerminated { get; private set; }

        public event EventHandler Terminated;

        public KeePassPowerToolExt()
        {
#if DEBUG
            Debug.AutoFlush = true;
            Debug.Listeners.Add(new TextWriterTraceListener("KeePassPowerTool.log"));
#endif
        }

        public override bool Initialize(IPluginHost host)
        {
            this.Host = host;
            
            this.ToolsMenuRoot = new ToolStripMenuItem("KeePass-PowerTool");
            host.MainWindow.ToolsMenu.DropDownItems.Add(this.ToolsMenuRoot);

            this.GroupContextMenuRoot = new ToolStripMenuItem("KeePass-PowerTool");
            host.MainWindow.GroupContextMenu.Items.Add(this.GroupContextMenuRoot);

            this.EntryContextMenuRoot = new ToolStripMenuItem("KeePass-PowerTool");
            host.MainWindow.EntryContextMenu.Items.Add(this.EntryContextMenuRoot);

            this.Initialize(new FaviconComponent(this));

            return true;
        }

        private void Initialize(EntryComponent component)
        {
            var host = this.Host;

            var tools = component.BuildMenuItemForToolsMenu().ToArray();
            if (tools.Length > 0)
            {
                this.ToolsMenuRoot.DropDownItems.AddSeparator();
                this.ToolsMenuRoot.DropDownItems.AddRange(tools);
            }

            var groups = component.BuildMenuItemForGroupContextMenu().ToArray();
            if (groups.Length > 0)
            {
                this.GroupContextMenuRoot.DropDownItems.AddSeparator();
                this.GroupContextMenuRoot.DropDownItems.AddRange(groups);
            }

            var entries = component.BuildMenuItemForEntryContextMenu().ToArray();
            if (entries.Length > 0)
            {
                this.EntryContextMenuRoot.DropDownItems.AddSeparator();
                this.EntryContextMenuRoot.DropDownItems.AddRange(entries);
            }
        }

        public void MakeModified()
        {
            this.Host.MainWindow.UpdateUI(false, null, false, null, true, null, true);
        }

        public override void Terminate()
        {
            this.IsTerminated = true;
            this.Terminated?.Invoke(this, EventArgs.Empty);
        }
    }
}
