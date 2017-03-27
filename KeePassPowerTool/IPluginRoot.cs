using KeePass.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeePassPowerTool
{
    interface IPluginRoot
    {
        IPluginHost Host { get; }

        bool IsTerminated { get; }

        event EventHandler Terminated;

        void MakeModified();
    }
}
