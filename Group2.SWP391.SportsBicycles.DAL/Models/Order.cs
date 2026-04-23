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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // nếu chưa có

        public DateTime? ExpiresAt { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;

        // buyer shipping info để auto shipment
        public int? ToDistrictId { get; set; }
        public string? ToWardCode { get; set; }

        public string? ToWardName { get; set; }
        public string? ToDistrictName { get; set; }
        public string? ToProvinceName { get; set; }

        public decimal? DistanceKm { get; set; }

        // tiền
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();

        public Shipment? Shipment { get; set; }
        public Transaction? Transaction { get; set; }
        public Review? Review { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? SellerNotifiedAt { get; set; }
        public DateTime? PayoutEligibleAt { get; set; }
        public DateTime? PaidOutAt { get; set; }

        public RefundInfo? RefundInfo { get; set; }
    }
}
