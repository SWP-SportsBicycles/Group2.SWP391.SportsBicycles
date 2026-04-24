using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Report : BaseEntity
    {
        public Guid Id { get; set; }

        public ReportTypeEnum Type { get; set; }
        public string Reason { get; set; } = default!;

        public string? Description { get; set; } 
        public string? VideoUrl { get; set; }    
        public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Pending;

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = default!;

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
    }
}
