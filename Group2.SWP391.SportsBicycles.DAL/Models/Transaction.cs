    using Group2.SWP391.SportsBicycles.Common.Enums;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Group2.SWP391.SportsBicycles.DAL.Models
    {
        public class Transaction : BaseEntity
        {
        public Guid Id { get; set; }

        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }

        public string OrderCode { get; set; } = default!;
        public string? ProviderOrderCode { get; set; }
        public string? PaymentLink { get; set; }

        public TransactionStatusEnum Status { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaidAt { get; set; }

        public string? Description { get; set; }

        // optional giữ nguyên
        public Guid? PolicyId { get; set; }
        public Policy? Policy { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }
    }
    }
