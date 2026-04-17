using Group2.SWP391.SportsBicycles.Common.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class CreateUserByAdminDto
    {
        [Required(ErrorMessage = "Họ và tên không được để trống.")]

        [MinLength(3, ErrorMessage = "Họ và tên phải có ít nhất 3 ký tự.")]
        public string FullName { get; set; } = null!;

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email phải đúng định dạng example@gmail.com.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$",
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, gồm chữ hoa, số và ký tự đặc biệt."
        )]
        [DefaultValue("Abc@123")]
        public string Password { get; set; } = null!;
    }
}
