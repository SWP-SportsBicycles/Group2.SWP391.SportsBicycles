using Group2.SWP391.SportsBicycles.Common.Helpers;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public interface IJwtService
    {
        (string token, DateTime expiresAt) GenerateAccessToken(User user);

        string GenerateResetPasswordToken(string email);
        string? ValidateAndGetEmailFromResetToken(string token);

    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public (string token, DateTime expiresAt) GenerateAccessToken(User user)
        {
            var issuer = _config["JWT:ValidIssuer"];
            var audience = _config["JWT:ValidAudience"];
            var secret = _config["JWT:Secret"];

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT:Secret is missing in configuration.");

            var expiresAt = DateTimeHelper.NowVN().AddDays(1);

            var claims = new List<Claim>
            {

                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),


                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),


                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim("PhoneNumber", user.PhoneNumber ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return (jwt, expiresAt);
        }

        public string GenerateResetPasswordToken(string email)
        {
            var secret = _config["JWT:Secret"];
            var issuer = _config["JWT:ValidIssuer"];
            var audience = _config["JWT:ValidAudience"];

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT:Secret is missing in configuration.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Email, email),
        new Claim("type", "reset_password")
    };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string? ValidateAndGetEmailFromResetToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var secret = _config["JWT:Secret"];
            var issuer = _config["JWT:ValidIssuer"];
            var audience = _config["JWT:ValidAudience"];

            if (string.IsNullOrWhiteSpace(secret))
                return null;

            var key = Encoding.UTF8.GetBytes(secret);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                var type = principal.FindFirst("type")?.Value;
                if (type != "reset_password")
                    return null;

                return principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
    ?? principal.FindFirst(ClaimTypes.Email)?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
