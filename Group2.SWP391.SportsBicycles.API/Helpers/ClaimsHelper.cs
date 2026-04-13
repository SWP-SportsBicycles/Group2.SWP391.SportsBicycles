using System.Security.Claims;

namespace Group2.SWP391.SportsBicycles.API.Helpers
{
    public static class ClaimsHelper
    {
        public static Guid GetUserId(HttpContext http)
        {
            var idStr =
                http.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                http.User.FindFirstValue("sub") ??
                http.User.FindFirstValue("userId");

            if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var id))
                throw new UnauthorizedAccessException("Không lấy được UserId từ token.");

            return id;
        }
    }
}
