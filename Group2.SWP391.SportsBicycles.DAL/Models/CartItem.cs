using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class CartItem
    {
        public Guid Id { get; set; }

        public Guid CartId { get; set; }
        public Cart Cart { get; set; } = default!;

        public Guid BikeId { get; set; }
        public Bike Bike { get; set; } = default!;

        public decimal UnitPrice { get; set; }
        public bool IsSelected { get; set; }
    }
}
