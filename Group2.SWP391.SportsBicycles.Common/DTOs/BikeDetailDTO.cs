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
        public string Brand { get; set; } = default!;
        public string Category { get; set; } = default!;
        public decimal Price { get; set; }

        public string FrameSize { get; set; } = default!;
        public string FrameMaterial { get; set; } = default!;
        public string Paint { get; set; } = default!;
        public string Groupset { get; set; } = default!;
        public string Operating { get; set; } = default!;
        public string TireRim { get; set; } = default!;
        public string BrakeType { get; set; } = default!;
        public string Overall { get; set; } = default!;

        public bool IsInspected { get; set; }
        public List<MediaDTO> Medias { get; set; } = new();
    }
}
