using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.Enums
{
    public enum ListingStatusEnum
    {
        Draft = 1,
        PendingInspection = 2,   // 🔥 thêm cái này
        PendingReview = 3,

        Published = 4,

        Reserved = 5,
        Withdrawn = 6,
        Rejected = 7
    }
}
