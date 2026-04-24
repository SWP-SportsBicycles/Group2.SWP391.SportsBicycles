using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.Enums
{
    public enum ReportTypeEnum
    {
        WrongDescription = 1,     // Không đúng mô tả
        ProductDefect = 2,        // Hư hỏng / Lỗi sản phẩm
        MissingOrWrongItem = 3,   // Thiếu phụ kiện / Sai hàng
        ShippingIssue = 4,        // Vấn đề vận chuyển
        Other = 5                 // Khác
    }
}
