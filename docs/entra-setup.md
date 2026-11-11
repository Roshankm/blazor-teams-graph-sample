# Connect the sample to Microsoft Graph

Without these settings the app runs in demo mode. With them, it signs users in with Microsoft Entra ID
and calls Microsoft Graph on their behalf.

You need a Microsoft 365 tenant with Teams. A free [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program) sandbox works.

## 1. Register the app

1. Open the [Entra admin center](https://entra.microsoft.com) → **App registrations** → **New registration**.
2. Name: `Teams Meetings Sample`. Supported account types: **this organizational directory only**.
3. Redirect URI: platform **Web**, value `https://localhost:7215/signin-oidc`.
4. After it is created, open **Authentication** and set the front-channel logout URL to
   `https://localhost:7215/signout-oidc`.

## 2. Add a client secret

**Certificates & secrets** → **New client secret**. Copy the value now; it is shown only once.

## 3. Grant Microsoft Graph permissions

**API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**:

| Permission | Why | Admin consent |
|---|---|---|
| `User.Read` | Sign in and show the user's name | No |
| `OnlineMeetings.ReadWrite` | Create meetings and look them up by join link | No |
| `OnlineMeetingRecording.Read.All` | List and download recordings | **Yes** |
| `OnlineMeetingTranscript.Read.All` | List and download transcripts | **Yes** |

Then click **Grant admin consent for &lt;tenant&gt;**.

## 4. Store the settings as user secrets

Never commit secrets. From the repository root:

```bash
cd src/TeamsMeetings.Web
dotnet user-secrets set "AzureAd:TenantId" "<directory (tenant) id>"
dotnet user-secrets set "AzureAd:ClientId" "<application (client) id>"
dotnet user-secrets set "AzureAd:ClientSecret" "<client secret value>"
dotnet run --launch-profile https
```

The demo banner disappears and the app asks you to sign in.

## Things that commonly go wrong

* **403 on recordings or transcripts:** admin consent has not been granted for the two `.Read.All` permissions.
* **Recording list is empty:** only the meeting **organiser** can read recordings through `/me/onlineMeetings`,
  the meeting must actually have been recorded, and processing takes a few minutes after it ends.
* **Transcript list is empty:** transcription must be turned on during the meeting.
* **"Find by join link" finds nothing:** the lookup only covers meetings the signed-in user organised.
* **Asked to sign in again after restarting the app:** tokens are cached in memory, so a restart clears them.
  The app catches the resulting `MsalUiRequiredException` and sends you back through sign-in automatically.
  Use a distributed token cache (Redis or SQL Server) in production.
