using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class ShipmentTracking : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = default!;

        public ShipmentStatusEnum Status { get; set; }

        public string Title { get; set; } = default!;       // ví dụ: Đang vận chuyển
        public string? Description { get; set; }            // ví dụ: Đơn hàng đang tới kho Hà Nội
        public string? Location { get; set; }               // ví dụ: Hà Nội Hub

        public DateTime EventTime { get; set; }             // thời điểm hãng ghi nhận
        public string? RawStatus { get; set; }              // trạng thái gốc từ hãng
    }
}
