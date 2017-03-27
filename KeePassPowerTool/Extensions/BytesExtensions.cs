using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KeePassPowerTool.Extensions
{
    static class BytesExtensions
    {
        private static SHA1 Sha1 { get; } = SHA1.Create();

        /// <summary>
        /// hash value of buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string Hash(this byte[] buffer)
        {
            var sha1 = Sha1.ComputeHash(buffer);
            return BitConverter.ToString(sha1).Replace("-", "");
        }
    }
}
