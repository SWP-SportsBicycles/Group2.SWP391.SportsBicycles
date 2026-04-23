using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class RefundInfoDTO
    {
        public string BankName { get; set; } = default!;
        public string BankAccountNumber { get; set; } = default!;
        public string BankAccountName { get; set; } = default!;
        public string? Note { get; set; }
    }
}
