using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class AdminUserListDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
 
        public DateTime CreatedAt { get; set; }
    }

    public class AdminUserDetailDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // ===== COMMON =====
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }

        // ===== SELLER =====
        public int? TotalListings { get; set; }
        public decimal? TotalRevenue { get; set; }

        // ===== BUYER =====
        public decimal? TotalSpent { get; set; }

        // ===== SHIPPING =====
        public string? SenderName { get; set; }
        public string? SenderPhone { get; set; }
        public string? SenderAddress { get; set; }

        // ===== BANK =====
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }
    }
}
