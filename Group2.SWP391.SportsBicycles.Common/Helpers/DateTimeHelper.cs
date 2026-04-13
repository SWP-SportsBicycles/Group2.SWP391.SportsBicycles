using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "SE Asia Standard Time"
                    : "Asia/Ho_Chi_Minh"
            );

        /// <summary>
        /// Giờ hiện tại theo Việt Nam (UTC+7)
        /// </summary>
        public static DateTime NowVN()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        /// <summary>
        /// Convert UTC → VN
        /// </summary>
        public static DateTime ToVN(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
        }

        /// <summary>
        /// Convert VN → UTC (khi cần lưu DB)
        /// </summary>
        public static DateTime ToUtc(DateTime vnTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(vnTime, VietnamTimeZone);
        }
    }
}
