using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.Enums
{
    public enum ShipmentStatusEnum
    {
        Pending = 0,      // mới tạo nội bộ
        Created = 1,      // đã tạo đơn bên vận chuyển
        PickingUp = 2,    // đang lấy hàng
        PickedUp = 3,     // đã lấy hàng
        InTransit = 4,    // đang vận chuyển
        Delivered = 5,    // đã giao
        Failed = 6,       // giao thất bại
        Cancelled = 7     // đã huỷ
    }
}
