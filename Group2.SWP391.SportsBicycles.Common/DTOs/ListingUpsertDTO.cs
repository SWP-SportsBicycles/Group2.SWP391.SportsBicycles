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
        // ===== LISTING =====
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;

        // ===== BIKE =====
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
    }

    public class ListingCreateDTO
    {
        // ===== LISTING =====
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;

        // ===== BIKE =====
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

        // ===== MEDIA =====
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
        public decimal Price { get; set; }

        public string Brand { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string FrameSize { get; set; } = default!;

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

        public BikeDetailDTO Bike { get; set; } = default!;
        public List<MediaDTO> Medias { get; set; } = new();
    }


    public class RejectListingDTO
    {
        public string Reason { get; set; } = default!;
    }
}
