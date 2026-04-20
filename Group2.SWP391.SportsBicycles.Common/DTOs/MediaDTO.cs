using Group2.SWP391.SportsBicycles.Common.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class MediaDTO
    {
        public string Image { get; set; }
        public string VideoUrl { get; set; }
        public MediaType Type { get; set; }
    }
    public class MediaCreateDTO
    {
        public IFormFile? File { get; set; }   // upload mới
        public string? Image { get; set; }     // fallback URL
        public string? VideoUrl { get; set; }

        public MediaType Type { get; set; }
    }
    public class UploadMediaRequest
    {
        public Guid ListingId { get; set; }
        public IFormFile File { get; set; } = default!;
        public MediaType Type { get; set; }
    }
}
