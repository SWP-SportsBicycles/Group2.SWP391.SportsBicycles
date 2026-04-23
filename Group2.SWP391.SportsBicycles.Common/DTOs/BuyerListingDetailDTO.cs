using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class BuyerListingDetailDTO
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string SellerName { get; set; } = default!;
        public bool IsWishlisted { get; set; }

        public List<BikeDetailDTO> Bikes { get; set; } = new();
    
}
}
