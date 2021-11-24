using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Singulink.Net.FileHosting.Utilities
{
    internal static class SecureGuid
    {
        private static readonly RNGCryptoServiceProvider _rng = new();

        public static Guid Create()
        {
            byte[] bytes = new byte[16];
            _rng.GetBytes(bytes);
            return new Guid(bytes);
        }
    }
}