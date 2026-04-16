using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class CreateShipmentDTO
    {
        public string ShippingProvider { get; set; } = default!;

        public string SenderName { get; set; } = default!;
        public string SenderPhone { get; set; } = default!;
        public string SenderAddress { get; set; } = default!;

        public decimal DistanceKm { get; set; }

        public string? Note { get; set; }
    }

    public class ShipmentTrackingDTO
    {
        public ShipmentStatusEnum Status { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime EventTime { get; set; }
    }

    public class ShipmentDetailDTO
    {
        public Guid ShipmentId { get; set; }
        public Guid OrderId { get; set; }
        public string ShippingProvider { get; set; } = default!;
        public string ShipmentCode { get; set; } = default!;
        public string? ProviderOrderCode { get; set; }
        public ShipmentStatusEnum Status { get; set; }
        public decimal ShippingFee { get; set; }

        public string SenderName { get; set; } = default!;
        public string SenderPhone { get; set; } = default!;
        public string SenderAddress { get; set; } = default!;

        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;

        public List<ShipmentTrackingDTO> Trackings { get; set; } = new();
    }
}