using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class ReviewDTO
    {
        public string ReviewerName { get; set; } = default!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string Date { get; set; } = default!;
    }
}
