using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KeePassPowerTool.Extensions
{
    static class StringExtensions
    {
        private static Regex linkRegex = new Regex("<link([^<>]*)>", RegexOptions.IgnoreCase);
        private static Regex iconRegex = new Regex("rel=\"(?:shortcut )?icon\"", RegexOptions.IgnoreCase);
        private static Regex hrefRegex = new Regex("href=\"([^\"]+)\"", RegexOptions.IgnoreCase);

        public static string TryParseIconUrl(this string html)
        {
            var ms = linkRegex.Matches(html);
            for (var i = 0; i < ms.Count; i++)
            {
                var v = ms[i].Value;
                if (iconRegex.Match(ms[i].Value).Success)
                {
                    var m = hrefRegex.Match(v);
                    if (m.Success) return m.Groups[1].Value;
                }
            }

            return null;
        }
    }
}
