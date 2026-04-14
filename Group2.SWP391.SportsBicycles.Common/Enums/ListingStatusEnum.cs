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
        PendingReview = 2,
        Published = 3,

        Reserved = 4,      // có deposit active order (locked)

        Withdrawn = 5,
        Rejected = 6
    }
}
