using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class BuyerBikeListingDTO
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = default!;
        public Guid BikeId { get; set; }
        public decimal Price { get; set; }
        public string Brand { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string Thumbnail { get; set; } = string.Empty;
        public string Overall { get; set; } = default!;
        public bool IsWishlisted { get; set; }
        public bool IsInspected { get; set; }
    }
}
