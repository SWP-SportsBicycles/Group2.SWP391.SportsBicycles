using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
    }
}
