using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class SellerShippingProfileDTO
    {
        public string SenderName { get; set; } = default!;
        public string SenderPhone { get; set; } = default!;
        public string SenderAddress { get; set; } = default!;
        public int FromDistrictId { get; set; }
        public string FromWardCode { get; set; } = default!;
        public string? FromWardName { get; set; }
        public string? FromDistrictName { get; set; }
        public string? FromProvinceName { get; set; }


        //  ADD BANK INFO
        public string BankName { get; set; } = default!;
        public string BankAccountNumber { get; set; } = default!;
        public string BankAccountName { get; set; } = default!;
    }
}
