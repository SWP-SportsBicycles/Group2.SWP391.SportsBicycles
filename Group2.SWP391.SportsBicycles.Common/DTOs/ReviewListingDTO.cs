using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class ReviewListingDTO
    {
        public string? Comment { get; set; }

        public bool Frame { get; set; }
        public bool PaintCondition { get; set; }
        public bool Drivetrain { get; set; }
        public bool Brakes { get; set; }

        public bool? ForcePass { get; set; }
        public bool? IsFlagged { get; set; }
    }
}
