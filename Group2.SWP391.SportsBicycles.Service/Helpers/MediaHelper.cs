using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Helpers
{
    public static class MediaHelper
    {
        public static bool IsVideo(string? videoUrl)
            => !string.IsNullOrEmpty(videoUrl);

        public static bool IsImage(string? imageUrl)
            => !string.IsNullOrEmpty(imageUrl);
    }
}
