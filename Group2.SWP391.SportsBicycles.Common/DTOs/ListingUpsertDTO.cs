using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class ListingUpsertDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class ListingCreateDTO
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;

        public string SerialNumber { get; set; } = default!;
        public string City { get; set; } = default!;

        public decimal Price { get; set; }

        public string Brand { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string FrameSize { get; set; } = default!;

        public List<MediaDTO> Medias { get; set; } = new();
    }

    public class ListingDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Status { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string City { get; set; } = default!;
        public string SerialNumber { get; set; } = default!;

        public decimal? Price { get; set; }
        public string? Thumbnail { get; set; }
    }

    public class ListingDetailsDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Status { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string City { get; set; } = default!;
        public string SerialNumber { get; set; } = default!;

        public List<BikeDetailDTO> Bikes { get; set; } = new();
    }
}
