using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Shipment : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = default!;

        public string ShippingProvider { get; set; } = default!; // GHN, GHTK...
        public string ShipmentCode { get; set; } = default!;     // mã nội bộ hệ thống
        public string? ProviderOrderCode { get; set; }           // mã đơn bên vận chuyển

        public ShipmentStatusEnum Status { get; set; } = ShipmentStatusEnum.Pending;

        public decimal ShippingFee { get; set; }

        public string SenderName { get; set; } = default!;
        public string SenderPhone { get; set; } = default!;
        public string SenderAddress { get; set; } = default!;
        public decimal DistanceKm { get; set; }
        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;

        public DateTime? PickupAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? FailedAt { get; set; }

        public string? FailReason { get; set; }
        public string? Note { get; set; }

        public ICollection<ShipmentTracking> Trackings { get; set; } = new List<ShipmentTracking>();
    }
}
