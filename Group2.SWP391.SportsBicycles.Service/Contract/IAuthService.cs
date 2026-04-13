using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IAuthService
    {
        Task<SignUpResult> SignUpAsync(SignUpDto dto);

        Task<LoginResult> SignInAsync(LoginDto request, string? ipAddress, string? deviceInfo);

        Task<LoginResult> RenewTokenAsync(string refreshToken, string? ipAddress, string? deviceInfo);

        Task SendOtpAsync(string email);
        Task<(bool Success, string Message)> VerifyOtpAsync(OtpVerifyDto dto);

        Task<SignUpResult> ResendOtpAsync(string email);

        Task<(bool Success, string Message, string? ErrorType)> LogoutAsync(string refreshToken);

        Task<LoginResult> GoogleSignInAsync(string idToken, RoleEnum role, string? ipAddress, string? deviceInfo);

        Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

        Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequestDto dto);

        Task<(bool Success, string Message)> ResetPasswordByLinkAsync(string token, ResetPasswordByLinkDto dto);

        Task<GetMeDto?> GetMeAsync(Guid userId);

        //Task<(bool Success, string Message, string? AvtUrl)> UploadAvatarAsync(Guid userId, IFormFile file);
    }
}
