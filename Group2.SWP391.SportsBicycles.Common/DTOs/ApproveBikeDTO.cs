using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class ApproveBikeDTO
    {
        public bool Frame { get; set; }
        public bool PaintCondition { get; set; }
        public bool Drivetrain { get; set; }
        public bool Brakes { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
    }
}
