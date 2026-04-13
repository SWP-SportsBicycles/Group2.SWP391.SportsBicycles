using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Helpers
{
    public static class OtpGenerator
    {
        public static string GenerateOtp(int length = 6)
        {
            var max = (int)Math.Pow(10, length);
            var value = RandomNumberGenerator.GetInt32(0, max);
            return value.ToString(new string('0', length));
        }
    }
}
