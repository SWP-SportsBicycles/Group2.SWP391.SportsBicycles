using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class BikePendingDTO
    {
        public Guid BikeId { get; set; }
        public Guid ListingId { get; set; }
        public string BikeName { get; set; }
        public string? Thumbnail { get; set; }
        public string SellerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
