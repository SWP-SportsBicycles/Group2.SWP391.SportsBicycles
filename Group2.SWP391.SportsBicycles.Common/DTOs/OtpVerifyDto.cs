using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class OtpVerifyDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP phải đúng 6 chữ số.")]
        public string Otp { get; set; } = null!;
    }
}
