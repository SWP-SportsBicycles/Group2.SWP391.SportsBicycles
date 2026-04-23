using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class SellerOrderDTO
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }

        public string BuyerName { get; set; }
        public string BuyerPhone { get; set; }

        public string ReceiverAddress { get; set; }

        public string BikeName { get; set; }
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsDelivered { get; set; }
        public bool IsPaidOut { get; set; }
        public DateTime? PaidOutAt { get; set; }

        public decimal PayoutAmount { get; set; }
    }
}
