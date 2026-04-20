using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaTypeEnum = Group2.SWP391.SportsBicycles.Common.Enums.MediaType;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Media : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid BikeId { get; set; }
        public Bike Bike { get; set; } = default!;

        public string? VideoUrl { get; set; }
        public string? Image { get; set; }

        // ✅ NEW
        public MediaTypeEnum Type { get; set; }
    }
}
