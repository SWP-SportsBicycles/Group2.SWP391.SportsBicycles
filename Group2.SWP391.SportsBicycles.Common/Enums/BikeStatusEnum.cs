using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.Enums
{
    public enum BikeStatusEnum
    {
        PendingInspection = 1,   // seller submit
        PendingReview = 2,       // inspector đã check xong
        Available = 3,
        Reserved = 4,
        Sold = 5,
        Disabled = 6
    }
}
