using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class OrderItem
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = default!;

        public Guid BikeId { get; set; }
        public Bike Bike { get; set; } = default!;

        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
