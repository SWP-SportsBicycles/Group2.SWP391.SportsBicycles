using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class WishlistDTO
    {
        public Guid BikeId { get; set; }
    }

    public class WishlistItemDTO
    {
        public Guid BikeId { get; set; }
        public Guid ListingId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Status { get; set; } = string.Empty; // Available / Sold / Hidden

        public string Thumbnail { get; set; } = string.Empty;
    }
}
