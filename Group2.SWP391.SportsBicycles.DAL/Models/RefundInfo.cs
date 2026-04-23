using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class RefundInfo : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public Guid UserId { get; set; }

        public string BankName { get; set; } = default!;
        public string BankAccountNumber { get; set; } = default!;
        public string BankAccountName { get; set; } = default!;

        public decimal RefundAmount { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
