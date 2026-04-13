using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Listing : BaseEntity
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public ListingStatusEnum Status { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public ICollection<Bike> Bikes { get; set; } = new List<Bike>();
    }
}
