using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface IEmailTemplateService
    {
        string BuildOtpEmail(string? fullName, string otp, int expiredMinutes);

        string BuildUserLockEmail(string? fullName);
        string BuildUserUnlockEmail(string? fullName);

        string BuildListingRejectedEmail(string? fullName, string listingTitle, string? reason);
        string BuildBikeRejectedEmail(string? fullName, string bikeName, string? reason);

        string BuildPaymentSuccessEmail(string? fullName, string orderCode, decimal totalAmount);

        string BuildOrderCompletedThankYouEmail(string? fullName, string orderCode);

        string BuildForgotPasswordEmail(string? fullName, string resetLink, int expiredMinutes);
        string BuildPasswordChangedEmail(string? fullName);
    }
}
