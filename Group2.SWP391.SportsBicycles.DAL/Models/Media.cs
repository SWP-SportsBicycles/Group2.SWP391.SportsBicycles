using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Media
    {
        public Guid Id { get; set; }

        public Guid BikeId { get; set; }
        public Bike Bike { get; set; } = default!;

        public string? VideoUrl { get; set; }
        public string? Image { get; set; }
    }
}
