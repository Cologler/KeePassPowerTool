using KeePass.Plugins;
using KeePassLib;
using KeePassPowerTool.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeePassPowerTool.Favicon
{
    class CustomIconAgent
    {
        private readonly IPluginHost host;
        private Dictionary<string, PwCustomIcon> iconsCached;

        public CustomIconAgent(IPluginHost host)
        {
            this.host = host;

            this.host.MainWindow.FileClosed += (s, e) => this.ClearCache();
        }

        private void EnsureCreated()
        {
            if (this.iconsCached != null) return;
            this.ReCreateCache();
        }

        public void ClearCache() => this.iconsCached = null;

        private void ReCreateCache()
        {
            if (!this.host.Database.IsOpen) return;

            if (this.iconsCached == null)
            {
                this.iconsCached = new Dictionary<string, PwCustomIcon>();
            }
            else
            {
                this.iconsCached.Clear();
            }

            foreach (var item in this.host.Database.CustomIcons.ToArray())
            {
                this.iconsCached[item.ImageDataPng.Hash()] = item;
            }
        }

        public PwUuid TryAddIcon(byte[] buffer)
        {
            this.EnsureCreated();

            var hash = buffer.Hash();

            PwUuid Impl()
            {
                if (!this.host.Database.IsOpen) throw new InvalidOperationException();

                if (this.iconsCached.TryGetValue(hash, out var icon))
                {
                    if (!this.host.Database.CustomIcons.Contains(icon))
                    {
                        // db changed.
                        this.ReCreateCache();
                        return Impl();
                    }
                    Debug.WriteLine($"exists icon.");
                }
                else
                {
                    Debug.WriteLine($"create icon.");
                    icon = new PwCustomIcon(new PwUuid(true), buffer);
                    this.host.Database.CustomIcons.Add(icon);
                    this.iconsCached[hash] = icon;
                }

                return icon.Uuid;
            }

            return Impl();
        }
    }
}
