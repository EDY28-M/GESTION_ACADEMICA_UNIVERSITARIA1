using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal? user, out int userId)
    {
        userId = default;

        if (user == null)
        {
            return false;
        }

        // Prefer standard claim used across the API and by SignalR's default IUserIdProvider.
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Backward compatibility for any tokens/clients using a custom "id" claim.
        if (string.IsNullOrWhiteSpace(value))
        {
            value = user.FindFirst("id")?.Value;
        }

        return int.TryParse(value, out userId);
    }
}
