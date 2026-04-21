using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.Enums
{
    public enum OrderStatusEnum
    {
        Locked = 0,      // 🔥 THÊM (QUAN TRỌNG NHẤT)
        Pending = 1,
        Paid = 2,
        Confirmed = 3,
        Shipping = 4,
        Delivered = 5,     // 👈 thêm
        Completed = 6,
        Cancelled = 7,
        Disputed = 8       // 👈 để làm report sau
    }
}
