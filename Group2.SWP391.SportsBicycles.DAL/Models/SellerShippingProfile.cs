using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class SellerShippingProfile : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public string SenderName { get; set; } = default!;
        public string SenderPhone { get; set; } = default!;
        public string SenderAddress { get; set; } = default!;

        public int FromDistrictId { get; set; }
        public string FromWardCode { get; set; } = default!;

        public string? FromWardName { get; set; }
        public string? FromDistrictName { get; set; }
        public string? FromProvinceName { get; set; }

        public bool IsDefault { get; set; } = true;
    }

}
