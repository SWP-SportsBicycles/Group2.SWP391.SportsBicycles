using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class SellerReviewDTO
    {
        public string SellerName { get; set; } = default!;
        public string JoinDate { get; set; } = default!;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ReviewDTO> LatestReviews { get; set; } = new();
    }
}
