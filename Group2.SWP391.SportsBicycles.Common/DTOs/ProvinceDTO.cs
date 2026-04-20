using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class ProvinceDTO
    {
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; } = default!;
    }

    public class DistrictDTO
    {
        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = default!;
    }

    public class WardDTO
    {
        public string WardCode { get; set; } = default!;
        public string WardName { get; set; } = default!;
    }
}
