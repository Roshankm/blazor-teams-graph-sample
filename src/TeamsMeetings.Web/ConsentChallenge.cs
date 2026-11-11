using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace TeamsMeetings.Web;

/// <summary>
/// Token acquisition fails with <see cref="MsalUiRequiredException"/> when the user must sign in
/// again or consent to a new scope - for example after the in-memory token cache is lost on restart
/// while the auth cookie is still valid. Graph wraps it, so the whole exception chain is checked.
/// </summary>
internal static class ConsentChallenge
{
    public static bool IsRequired(Exception? exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex is MicrosoftIdentityWebChallengeUserException or MsalUiRequiredException)
            {
                return true;
            }
        }

        return false;
    }
}
