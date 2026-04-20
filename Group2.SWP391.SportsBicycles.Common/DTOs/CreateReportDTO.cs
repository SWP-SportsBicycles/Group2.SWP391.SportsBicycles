using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class CreateReportDTO
    {
        [Required(ErrorMessage = "Loại report là bắt buộc")]
        public ReportTypeEnum Type { get; set; }

        [Required(ErrorMessage = "Lý do report là bắt buộc")]
        [MaxLength(1000, ErrorMessage = "Lý do report tối đa 1000 ký tự")]
        public string Reason { get; set; } = default!;
    }


    public class ReportDetailDTO
    {
        public Guid ReportId { get; set; }
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = default!;
        public string Reason { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class ReportListItemDTO
    {
        public Guid ReportId { get; set; }
        public Guid OrderId { get; set; }
        public string Type { get; set; } = default!;
        public string Reason { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
