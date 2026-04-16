using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Order : BaseEntity
    {
        public Guid Id { get; set; }

        public OrderStatusEnum Status { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;

        public decimal TotalAmount { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public ICollection<Report> Reports { get; set; } = new List<Report>();

        public Shipment? Shipment { get; set; }

        // 1-1 transaction
        public Transaction? Transaction { get; set; }

        public Review? Review { get; set; }
    }
}
