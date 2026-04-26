using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class User : BaseEntity
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? AvtUrl { get; set; }
        public string? FirebaseUID { get; set; }
        public int WarningCount { get; set; } = 0;
        public RoleEnum Role { get; set; }
        public decimal WalletBalance { get; set; }

        public UserStatusEnum Status { get; set; } = UserStatusEnum.InActive;
        public string? PickupAddress { get; set; }
        public int? PickupDistrictId { get; set; }
        public string? PickupWardCode { get; set; }

        public string? PickupWardName { get; set; }
        public string? PickupDistrictName { get; set; }
        public string? PickupProvinceName { get; set; }
        public Cart? Cart { get; set; }
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<Report> Reports { get; set; } = new List<Report>();

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        public ICollection<SellerShippingProfile> SellerShippingProfiles { get; set; } = new List<SellerShippingProfile>();
    }
}
