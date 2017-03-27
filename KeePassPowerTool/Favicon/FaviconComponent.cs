using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeePassLib;
using KeePassPowerTool.Extensions;
using System.Diagnostics;

namespace KeePassPowerTool.Favicon
{
    class FaviconComponent : EntryComponent
    {
        public CustomIconAgent CustomIconAgent { get; }

        public FaviconComponent(IPluginRoot root) : base(root)
        {
            this.CustomIconAgent = new CustomIconAgent(root.Host);
        }

        public override IEnumerable<ToolStripMenuItem> BuildMenuItemForEntryContextMenu()
        {
            var mi = new ToolStripMenuItem("download favicons");
            mi.Click += this.OnExecuteSelected;
            return new[] { mi };
        }

        public override IEnumerable<ToolStripMenuItem> BuildMenuItemForGroupContextMenu()
        {
            var mi = new ToolStripMenuItem("download favicons");
            mi.Click += this.OnExecuteGroup;
            return new[] { mi };
        }

        public override IEnumerable<ToolStripMenuItem> BuildMenuItemForToolsMenu()
        {
            var mi = new ToolStripMenuItem("download all favicons");
            mi.Click += this.OnExecuteAll;

            var removeUnused = new ToolStripMenuItem("remove unused favicons");
            removeUnused.Click += this.OnRemoveUnused;

            return new[] { mi, removeUnused };
        }

        private FaviconDownloader downloader;

        protected override void Execute(PwEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;

            if (this.downloader == null)
            {
                Debug.WriteLine($"begin download: <{entries.Length}>");

                var logger = this.Root.Host.MainWindow.CreateStatusBarLogger();
                var dlr = new FaviconDownloader(this);
                dlr.Completed += (s, e) =>
                {
                    if (this.downloader == e) this.downloader = null;
                    if (this.Root.IsTerminated) return;
                    logger.EndLogging();
                    Debug.WriteLine($"end download.");
                    this.Root.Host.MainWindow.UpdateUI(true, null, false, null, false, null, false);
                };
                this.downloader = dlr;
                dlr.Enqueue(entries);
                logger.StartLogging("download ...", false);
                dlr.Start(logger);
            }
            else
            {
                Debug.Assert(this.downloader.ConnectionInfo == this.Root.Host.Database.IOConnectionInfo);
                Debug.WriteLine($"append download: <{entries.Length}>");

                this.downloader.Enqueue(entries);
            }
        }

        protected void OnRemoveUnused(object sender, EventArgs e)
        {
            if (!this.Root.Host.Database.IsOpen) return;

            this.CustomIconAgent.ClearCache();
            this.Root.Host.Database.DeleteUnusedCustomIcons();
            this.Root.MakeModified();
        }
    }
}
