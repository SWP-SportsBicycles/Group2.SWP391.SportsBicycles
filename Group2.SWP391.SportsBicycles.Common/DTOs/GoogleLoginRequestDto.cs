using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class GoogleLoginRequestDto
    {
        public string IdToken { get; set; } = default!;
        public RoleEnum Role { get; set; }
    }
}
