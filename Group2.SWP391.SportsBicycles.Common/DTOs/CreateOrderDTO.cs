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

        public int ToProvinceId { get; set; }          // ✅ thêm
        public string ToProvinceName { get; set; } = default!; // ✅ thêm

        public int ToDistrictId { get; set; }
        public string ToDistrictName { get; set; } = default!; // ✅ thêm

        public string ToWardCode { get; set; } = default!;
        public string ToWardName { get; set; } = default!; // ✅ thêm
    }
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

