# Identity email and password recovery

The API uses ASP.NET Core Identity Minimal APIs and Resend for account email.
The default policy requires email confirmation before login. Access and refresh
tokens use Identity's bearer scheme; do not assume JWT format.

## Configuration

The API validates four Email settings at startup:

| Setting | Purpose |
| --- | --- |
| ApiKey | Resend sending key |
| FromAddress | Valid sender on your verified domain |
| FromName | Sender display name |
| PublicWebBaseUrl | Externally reachable absolute HTTPS Web origin |

Use the [README setup commands](../README.md#2-configure-api-secrets).
Environment variables use double underscores, such as Email__PublicWebBaseUrl.
Local Web is https://localhost:7056; development Web is https://dev.catchlogr.com.
Use the Web origin, not the API origin. Mobile backend selection does not
configure email links. A local link works only on a device that can reach it.

## Flow

1. Registration sends a confirmation link and opens Mobile's check-email page,
   which can resend the message.
2. ResendIdentityEmailSender rewrites account-action links to Web Razor Pages.
3. The user confirms via Web and returns to Mobile to sign in.
4. Forgot password sends a reset link without revealing whether the account
   exists; the email also includes a code for the mobile reset form.
5. The Web reset page sends the email, reset code, and password to the API over
   HTTPS; the API validates the Identity code and password rules.

Web uses IIdentityApiClient, not database access. It sets Referrer-Policy to
no-referrer to limit account-action URL leakage. Keep codes and tokens out of logs.

## Verification

Use the [validation guide](VALIDATION.md): API integration tests exercise real
Identity confirmation/reset tokens with a capturing email sender; Web behavior
is covered in Catchlogr.Tests and Mobile flows in Catchlogr.Mobile.Tests.
These checks do not prove Resend delivery. When changing delivery/configuration,
perform a separate smoke check using the configured verified sender and confirm
the recipient can reach Web and complete the action.
