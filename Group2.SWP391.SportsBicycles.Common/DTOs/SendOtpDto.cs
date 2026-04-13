using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class SendOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}
