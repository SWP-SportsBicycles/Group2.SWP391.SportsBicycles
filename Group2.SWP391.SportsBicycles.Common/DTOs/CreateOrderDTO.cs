using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class CreateOrderDTO
    {
        public Guid BikeId { get; set; }

        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;

        public int ToDistrictId { get; set; }          // thêm
        public string ToWardCode { get; set; } = default!; // thêm

        public decimal DistanceKm { get; set; }
    }

    public class PreviewCheckoutDTO
    {
        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;
        public int ToDistrictId { get; set; }          // thêm
        public string ToWardCode { get; set; } = default!; // thêm

        public decimal DistanceKm { get; set; }
    }
}
