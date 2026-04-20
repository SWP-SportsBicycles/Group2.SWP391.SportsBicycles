using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class AdminListingDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string City { get; set; }

        public decimal Price { get; set; }
        public string Brand { get; set; }

        public string Status { get; set; }

        public string? Thumbnail { get; set; }
        public int TotalImages { get; set; }
        public bool HasVideo { get; set; }
    }
}
