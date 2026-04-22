using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class AdminOrderListDTO
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = default!;
        public decimal TotalAmount { get; set; }

        public string BikeTitle { get; set; } = default!;
        public string SellerName { get; set; } = default!;

        public DateTime? CompletedAt { get; set; }
        public DateTime? PaidOutAt { get; set; }
    }
}
