using FirebaseAdmin.Auth;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Common.Helpers;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Group2.SWP391.SportsBicycles.Services.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<RefreshToken> _refreshRepo;
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;
        private readonly IEmailService _email;
        private readonly IMemoryCache _cache;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _config;
       

        private const int OTP_MINUTES = 2;
        private const int REFRESH_DAYS = 7;
        private const int RESET_PASSWORD_MINUTES = 15;

        public AuthService(
            IGenericRepository<User> userRepo,
            IGenericRepository<RefreshToken> refreshRepo,
            IUnitOfWork uow,
            IJwtService jwt,
            IEmailService email,
            IMemoryCache cache,
            IEmailTemplateService emailTemplateService,
            IConfiguration config
            )
        {
            _userRepo = userRepo;
            _refreshRepo = refreshRepo;
            _uow = uow;
            _jwt = jwt;
            _email = email;
            _cache = cache;
            _emailTemplateService = emailTemplateService;
            _config = config;
            
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLower();
        private static string OtpCacheKey(string email) => $"otp:{NormalizeEmail(email)}";

        public async Task<SignUpResult> SignUpAsync(SignUpDto dto)
        {
            var email = NormalizeEmail(dto.Email);

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existedPhone = await _userRepo.GetFirstByExpression(u => u.PhoneNumber == dto.PhoneNumber);
                if (existedPhone != null)
                    return new SignUpResult { Success = false, Message = "Số điện thoại này đã tồn tại." };
            }

            var existed = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (existed != null)
                return new SignUpResult { Success = false, Message = "Email này đã tồn tại." };

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
                Email = email,
                Password = PasswordHasher.Hash(dto.Password),
                Role = dto.Role,
                Status = UserStatusEnum.InActive,
                WalletBalance = 0m
            };

            await _userRepo.Insert(user);
            await _uow.SaveChangeAsync();

            await SendOtpAsync(email);

            return new SignUpResult
            {
                Success = true,
                Message = "Đăng ký thành công. Vui lòng nhập mã OTP để xác thực Email."
            };
        }


        public async Task SendOtpAsync(string email)
        {
            var normalized = NormalizeEmail(email);

            var user = await _userRepo.GetFirstByExpression(u => u.Email == normalized);
            if (user == null) return;

            if (user.Status == UserStatusEnum.Active) return;

            var otp = OtpGenerator.GenerateOtp(6);
            _cache.Set(OtpCacheKey(normalized), otp, TimeSpan.FromMinutes(OTP_MINUTES));

            var html = _emailTemplateService.BuildOtpEmail(user.FullName, otp, OTP_MINUTES);

            await _email.SendEmailAsync(
                normalized,
                "Xác thực tài khoản SportsBicyclesStore",
                html,
                true
            );
        }

        public async Task<(bool Success, string Message)> VerifyOtpAsync(OtpVerifyDto dto)
        {
            var email = NormalizeEmail(dto.Email);
            var key = OtpCacheKey(email);

            if (!_cache.TryGetValue(key, out string? cachedOtp) || cachedOtp != dto.Otp)
                return (false, "OTP không hợp lệ hoặc đã hết hạn.");

            var user = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (user == null) return (false, "Người dùng không tồn tại.");

            if (user.Status == UserStatusEnum.Banned)
                return (false, "Tài khoản đã bị khóa.");

            user.Status = UserStatusEnum.Active;
            await _userRepo.Update(user);
            await _uow.SaveChangeAsync();

            _cache.Remove(key);
            return (true, "Xác thực thành công! Tài khoản đã được Active.");
        }

        public async Task<LoginResult> SignInAsync(LoginDto request, string? ipAddress, string? deviceInfo)
        {
            var email = NormalizeEmail(request.Email);

            var user = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (user == null)
                return new LoginResult { Success = false, Message = "Email này chưa được đăng ký." };

            if (user.Status == UserStatusEnum.InActive)
                return new LoginResult { Success = false, Message = "Tài khoản chưa xác minh email. Vui lòng nhập OTP để kích hoạt." };

            if (user.Status == UserStatusEnum.Banned)
                return new LoginResult { Success = false, Message = "Tài khoản đã bị khóa." };

            if (!PasswordHasher.Verify(request.Password, user.Password))
                return new LoginResult { Success = false, Message = "Mật khẩu không đúng." };

            var oldTokens = await _refreshRepo.GetAllDataByExpression(
                r => r.UserId == user.Id && !r.Revoked,
                0, 0, null, true
            );

            if (oldTokens.Items != null)
            {
                foreach (var old in oldTokens.Items)
                {
                    old.Revoked = true;
                    await _refreshRepo.Update(old);
                }
            }

            var refreshToken = RefreshTokenGenerator.Generate();

            var refreshEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                CreatedAt = DateTimeHelper.NowVN(),
                ExpiredAt = DateTimeHelper.NowVN().AddDays(REFRESH_DAYS),
                Revoked = false,
                IpAddress = ipAddress ?? "unknown",
                DeviceInfo = deviceInfo ?? "unknown"
            };

            await _refreshRepo.Insert(refreshEntity);
            await _uow.SaveChangeAsync();

            var (accessToken, _) = _jwt.GenerateAccessToken(user);

            return new LoginResult
            {
                Success = true,
                Message = "Đăng nhập thành công",
                Token = accessToken,
                RefreshToken = refreshToken,
                Role = user.Role.ToString()
            };
        }


        public async Task<LoginResult> RenewTokenAsync(string refreshToken, string? ipAddress, string? deviceInfo)
        {
            var stored = await _refreshRepo.GetFirstByExpression(r => r.Token == refreshToken);

            if (stored == null || stored.Revoked)
                return new LoginResult { Success = false, Message = "Refresh token không hợp lệ." };

            if (stored.ExpiredAt <= DateTimeHelper.NowVN())
            {
                stored.Revoked = true;
                await _refreshRepo.Update(stored);
                await _uow.SaveChangeAsync();
                return new LoginResult { Success = false, Message = "Refresh token đã hết hạn." };
            }

            var user = await _userRepo.GetById(stored.UserId);
            if (user == null)
                return new LoginResult { Success = false, Message = "Người dùng không tồn tại." };

            if (user.Status != UserStatusEnum.Active)
                return new LoginResult { Success = false, Message = "Tài khoản chưa được kích hoạt." };


            stored.Revoked = true;
            stored.IpAddress = ipAddress ?? stored.IpAddress;
            stored.DeviceInfo = deviceInfo ?? stored.DeviceInfo;
            await _refreshRepo.Update(stored);

            var newRefresh = RefreshTokenGenerator.Generate();
            var newEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefresh,
                CreatedAt = DateTimeHelper.NowVN(),
                ExpiredAt = DateTimeHelper.NowVN().AddDays(REFRESH_DAYS),
                Revoked = false,
                IpAddress = ipAddress ?? "unknown",
                DeviceInfo = deviceInfo ?? "unknown"
            };
            await _refreshRepo.Insert(newEntity);

            var (newAccess, _) = _jwt.GenerateAccessToken(user);
            await _uow.SaveChangeAsync();

            return new LoginResult
            {
                Success = true,
                Message = "Renew thành công",
                Token = newAccess,
                RefreshToken = newRefresh,
                Role = user.Role.ToString()
            };
        }

        public async Task<SignUpResult> ResendOtpAsync(string email)
        {
            var normalized = NormalizeEmail(email);

            var user = await _userRepo.GetFirstByExpression(u => u.Email == normalized);
            if (user == null)
                return new SignUpResult { Success = false, Message = "Email chưa được đăng ký." };

            if (user.Status == UserStatusEnum.Banned)
                return new SignUpResult { Success = false, Message = "Tài khoản đã bị khóa." };

            if (user.Status == UserStatusEnum.Active)
                return new SignUpResult { Success = false, Message = "Tài khoản đã xác thực email rồi." };


            await SendOtpAsync(normalized);

            return new SignUpResult
            {
                Success = true,
                Message = "Đã gửi lại mã OTP. Vui lòng kiểm tra email để xác thực."
            };
        }


        public async Task<(bool Success, string Message, string? ErrorType)> LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return (false, "Refresh token không được để trống.", "Invalid");

            var storedToken = await _refreshRepo.GetFirstByExpression(x => x.Token == refreshToken);

            if (storedToken == null)
                return (false, "Refresh token không hợp lệ.", "Invalid");

            if (storedToken.Revoked)
                return (false, "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.", "Expired");

            if (storedToken.ExpiredAt <= DateTimeHelper.NowVN())
                return (false, "Refresh token đã hết hạn.", "Expired");

            storedToken.Revoked = true;
            await _refreshRepo.Update(storedToken);
            await _uow.SaveChangeAsync();

            return (true, "Đăng xuất thành công.", null);
        }


        public async Task<LoginResult> GoogleSignInAsync(string idToken, RoleEnum role, string? ipAddress, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "IdToken không được để trống."
                };
            }

            // Chỉ cho phép login Google với 2 role này
            if (role != RoleEnum.BUYER && role != RoleEnum.SELLER)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Role không hợp lệ."
                };
            }

            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

                var firebaseUid = decodedToken.Uid;
                var email = decodedToken.Claims.ContainsKey("email")
                    ? decodedToken.Claims["email"]?.ToString()?.Trim().ToLower()
                    : null;

                var fullName = decodedToken.Claims.ContainsKey("name")
                    ? decodedToken.Claims["name"]?.ToString()?.Trim()
                    : null;

                var avatar = decodedToken.Claims.ContainsKey("picture")
                    ? decodedToken.Claims["picture"]?.ToString()
                    : null;

                if (string.IsNullOrWhiteSpace(firebaseUid))
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Không lấy được Firebase UID."
                    };
                }

                User? user = await _userRepo.GetFirstByExpression(x => x.FirebaseUID == firebaseUid);

                if (user == null && !string.IsNullOrWhiteSpace(email))
                {
                    user = await _userRepo.GetFirstByExpression(x => x.Email == email);

                    if (user != null)
                    {
                        user.FirebaseUID = firebaseUid;

                        if (string.IsNullOrWhiteSpace(user.AvtUrl) && !string.IsNullOrWhiteSpace(avatar))
                            user.AvtUrl = avatar;

                        if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(fullName))
                            user.FullName = fullName;

                        if (user.Status == UserStatusEnum.InActive)
                            user.Status = UserStatusEnum.Active;

                        await _userRepo.Update(user);
                        await _uow.SaveChangeAsync();
                    }
                }

                // Chưa có account => tạo mới theo role FE chọn
                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        FirebaseUID = firebaseUid,
                        Email = email ?? string.Empty,
                        FullName = !string.IsNullOrWhiteSpace(fullName) ? fullName : "Google User",
                        PhoneNumber = string.Empty,
                        Password = string.Empty,
                        AvtUrl = avatar,
                        Role = role,
                        WalletBalance = 0,
                        Status = UserStatusEnum.Active
                    };

                    await _userRepo.Insert(user);
                    await _uow.SaveChangeAsync();
                }
                else
                {
                    if (user.Status == UserStatusEnum.Banned)
                    {
                        return new LoginResult
                        {
                            Success = false,
                            Message = "Tài khoản đã bị khóa."
                        };
                    }

                    // Nếu account đã tồn tại mà role login khác role account
                    if (user.Role != role)
                    {
                        return new LoginResult
                        {
                            Success = false,
                            Message = $"Tài khoản này đã đăng ký với vai trò {user.Role}, không thể đăng nhập với vai trò {role}."
                        };
                    }
                }

                var oldTokens = await _refreshRepo.GetAllDataByExpression(
                    x => x.UserId == user.Id && !x.Revoked,
                    0, 0, null, true
                );

                if (oldTokens.Items != null)
                {
                    foreach (var item in oldTokens.Items)
                    {
                        item.Revoked = true;
                        await _refreshRepo.Update(item);
                    }
                }

                var refreshToken = RefreshTokenGenerator.Generate();

                var refreshEntity = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = refreshToken,
                    CreatedAt = DateTimeHelper.NowVN(),
                    ExpiredAt = DateTimeHelper.NowVN().AddDays(7),
                    Revoked = false,
                    IpAddress = ipAddress,
                    DeviceInfo = deviceInfo
                };

                await _refreshRepo.Insert(refreshEntity);
                await _uow.SaveChangeAsync();

                var (accessToken, _) = _jwt.GenerateAccessToken(user);

                return new LoginResult
                {
                    Success = true,
                    Message = "Đăng nhập Google thành công.",
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = $"Google login thất bại: {ex.Message}"
                };
            }
        }


        public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userRepo.GetById(userId);
            if (user == null)
                return (false, "Người dùng không tồn tại.");

            if (string.IsNullOrWhiteSpace(user.Password))
                return (false, "Tài khoản này đăng nhập bằng Google, không thể đổi mật khẩu theo cách này.");

            if (!PasswordHasher.Verify(dto.CurrentPassword, user.Password))
                return (false, "Mật khẩu hiện tại không đúng.");

            if (dto.NewPassword != dto.ConfirmPassword)
                return (false, "Mật khẩu xác nhận không khớp.");

            if (dto.CurrentPassword == dto.NewPassword)
                return (false, "Mật khẩu mới không được trùng với mật khẩu hiện tại.");

            user.Password = PasswordHasher.Hash(dto.NewPassword);

            await _userRepo.Update(user);
            await _uow.SaveChangeAsync();

            var html = _emailTemplateService.BuildPasswordChangedEmail(user.FullName);
            await _email.SendEmailAsync(user.Email, "Mật khẩu SportsBicyclesStore đã được thay đổi", html, true);

            return (true, "Đổi mật khẩu thành công.");
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
        {
            var email = NormalizeEmail(dto.Email);

            var user = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (user == null)
                return (false, "Email không tồn tại trong hệ thống.");

            if (string.IsNullOrWhiteSpace(user.Password))
                return (false, "Tài khoản này đăng nhập bằng Google. Vui lòng sử dụng đăng nhập Google.");

            if (user.Status == UserStatusEnum.Banned)
                return (false, "Tài khoản đã bị khóa.");

            var token = _jwt.GenerateResetPasswordToken(user.Email);

            var resetPasswordUrl = _config["AppSettings:ResetPasswordUrl"];
            if (string.IsNullOrWhiteSpace(resetPasswordUrl))
                resetPasswordUrl = "http://localhost:3000/reset-password";

            var resetLink = $"{resetPasswordUrl}?token={Uri.EscapeDataString(token)}";

            var html = _emailTemplateService.BuildForgotPasswordEmail(user.FullName, resetLink, RESET_PASSWORD_MINUTES);

            await _email.SendEmailAsync(
                user.Email,
                "Yêu cầu đặt lại mật khẩu SportsBicyclesStore",
                html,
                true
            );

            return (true, "Link đặt lại mật khẩu đã được gửi tới email.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordByLinkAsync(string token, ResetPasswordByLinkDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return (false, "Mật khẩu xác nhận không khớp.");

            var email = _jwt.ValidateAndGetEmailFromResetToken(token);
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Token không hợp lệ hoặc đã hết hạn.");

            var normalizedEmail = NormalizeEmail(email);

            var user = await _userRepo.GetFirstByExpression(u => u.Email == normalizedEmail);
            if (user == null)
                return (false, "Người dùng không tồn tại.");

            if (string.IsNullOrWhiteSpace(user.Password))
                return (false, "Tài khoản này đăng nhập bằng Google, không thể đặt lại mật khẩu theo cách này.");

            user.Password = PasswordHasher.Hash(dto.NewPassword);

            await _userRepo.Update(user);

            var oldTokens = await _refreshRepo.GetAllDataByExpression(
                r => r.UserId == user.Id && !r.Revoked,
                0, 0, null, true
            );

            if (oldTokens.Items != null)
            {
                foreach (var item in oldTokens.Items)
                {
                    item.Revoked = true;
                    await _refreshRepo.Update(item);
                }
            }

            await _uow.SaveChangeAsync();

            var html = _emailTemplateService.BuildPasswordChangedEmail(user.FullName);
            await _email.SendEmailAsync(user.Email, "Mật khẩu SportsBicyclesStore đã được thay đổi", html, true);

            return (true, "Đặt lại mật khẩu thành công.");
        }

        public async Task<GetMeDto?> GetMeAsync(Guid userId)
        {
            var user = await _userRepo.GetById(userId);

            if (user == null || user.IsDeleted)
                return null;

            return new GetMeDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvtUrl = user.AvtUrl,
                WalletBalance = user.WalletBalance,
                Role = user.Role.ToString()
            };
        }

        //public async Task<(bool Success, string Message, string? AvtUrl)> UploadAvatarAsync(Guid userId, IFormFile file)
        //{
        //    var user = await _userRepo.GetById(userId);
        //    if (user == null)
        //        return (false, "Người dùng không tồn tại.", null);

        //    try
        //    {
        //        var url = await _cloudinary.UploadAvatarAsync(userId, file);

        //        user.AvtUrl = url;
        //        user.UpdatedAt = DateTimeHelper.NowVN();

        //        await _userRepo.Update(user);
        //        await _uow.SaveChangeAsync();

        //        return (true, "Upload avatar thành công.", url);
        //    }
        //    catch (Exception ex)
        //    {
        //        return (false, ex.Message, null);
        //    }
        //}

    }
}
