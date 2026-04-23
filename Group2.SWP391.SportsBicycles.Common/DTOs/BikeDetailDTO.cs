using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class BikeDetailDTO
    {
        public Guid BikeId { get; set; }

        public string SerialNumber { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string Brand { get; set; } = default!;
        public string FrameSize { get; set; } = default!;
        public string FrameMaterial { get; set; } = default!;
        public string Condition { get; set; } = default!;

        public string Paint { get; set; } = default!;
        public string Groupset { get; set; } = default!;
        public string Operating { get; set; } = default!;
        public string TireRim { get; set; } = default!;
        public string BrakeType { get; set; } = default!;
        public string Overall { get; set; } = default!;

        public decimal Price { get; set; }
        public string City { get; set; } = default!;
        public string Status { get; set; } = default!;

        // NEW: media contract
        public string Thumbnail { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
        public List<string> VideoUrls { get; set; } = new();
    }
}
