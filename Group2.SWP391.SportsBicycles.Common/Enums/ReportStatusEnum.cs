using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.Enums
{
    public enum ReportStatusEnum
    {
        Pending = 1,    // Buyer vừa gửi report
        Reviewing = 2,  // Inspector đã check, gửi Admin duyệt
        Resolved = 3,   // Admin chấp thuận khiếu nại
        Rejected = 4    // Admin từ chối khiếu nại
    }
}
