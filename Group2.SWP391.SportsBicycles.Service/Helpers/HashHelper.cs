using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Helpers
{
    public static class HashHelper
    {
        public static string Hash(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(input))
            );
        }

        public static bool Verify(string input, string storedHash)
        {
            return Hash(input) == storedHash;
        }
    }
}
