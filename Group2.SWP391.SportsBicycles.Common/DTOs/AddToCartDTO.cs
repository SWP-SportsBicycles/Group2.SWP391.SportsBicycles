using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class AddToCartDTO
    {
        public Guid BikeId { get; set; }
    }

    public class UpdateCartItemSelectionDTO
    {
        public Guid CartItemId { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CreateOrderFromCartDTO
    {
        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;
        public int ToDistrictId { get; set; }           // 🔥 thêm
        public string ToWardCode { get; set; } = string.Empty; // 🔥 thêm

        public decimal? DistanceKm { get; set; }        // 🔥 để dùng HasValue / Value
    }
    public class BulkUpdateCartSelectionDTO
    {
        public List<Guid> CartItemIds { get; set; } = new();
        public bool IsSelected { get; set; }
    }

}
