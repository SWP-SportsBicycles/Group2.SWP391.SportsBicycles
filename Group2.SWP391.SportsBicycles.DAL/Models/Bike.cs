using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Bike : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid ListingId { get; set; }
        public Listing Listing { get; set; } = default!;

        public Guid? InspectionId { get; set; }
        public Inspection? Inspection { get; set; }

        public string Category { get; set; } = default!;
        public string Brand { get; set; } = default!;
        public string FrameSize { get; set; } = default!;
        public string FrameMaterial { get; set; } = default!;
        public string Paint { get; set; } = default!;
        public string Groupset { get; set; } = default!;
        public string Operating { get; set; } = default!;
        public string TireRim { get; set; } = default!;
        public string BrakeType { get; set; } = default!;
        public string Overall { get; set; } = default!;

        public BikeStatusEnum Status { get; set; }
        public decimal Price { get; set; }

        public ICollection<Media> Medias { get; set; } = new List<Media>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    }
}
