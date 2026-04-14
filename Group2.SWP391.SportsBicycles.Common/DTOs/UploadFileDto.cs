using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class UploadFileDto
    {
        public IFormFile File { get; set; } = null!;
        
    }
}
