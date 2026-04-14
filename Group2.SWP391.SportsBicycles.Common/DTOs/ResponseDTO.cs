using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class ResponseDTO
    {
        public bool IsSucess { get; set; } = true;
        public object Data { get; set; }

        public BusinessCode.BusinessCode BusinessCode { get; set; }
        public string Message { get; set; }
    }
}
