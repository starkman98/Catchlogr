# Identity email and password recovery

Catchlogr uses ASP.NET Core Identity's minimal API endpoints and Resend for
transactional account email. New accounts must confirm their email before
login.

## Required API configuration

The API reads these settings from the standard ASP.NET Core configuration
pipeline:

    Email:ApiKey
    Email:FromAddress
    Email:FromName
    Email:PublicWebBaseUrl

- ApiKey is the Resend sending API key.
- FromAddress must belong to the verified Resend domain, for example
  account@mail.catchlogr.com.
- FromName is the sender display name, normally Catchlogr.
- PublicWebBaseUrl is the externally reachable HTTPS web origin, such as
  https://dev.catchlogr.com. Identity account-action links are rewritten to
  user-facing Razor Pages on this origin before sending.

For local development, use user secrets:

    dotnet user-secrets set "Email:ApiKey" "<resend-api-key>" --project src/Catchlogr.Api
    dotnet user-secrets set "Email:FromAddress" "account@mail.catchlogr.com" --project src/Catchlogr.Api
    dotnet user-secrets set "Email:FromName" "Catchlogr" --project src/Catchlogr.Api
    dotnet user-secrets set "Email:PublicWebBaseUrl" "https://dev.catchlogr.com" --project src/Catchlogr.Api

In an environment file or container configuration, use ASP.NET Core's
double-underscore form:

    Email__ApiKey=...
    Email__FromAddress=account@mail.catchlogr.com
    Email__FromName=Catchlogr
    Email__PublicWebBaseUrl=https://dev.catchlogr.com

Never commit the API key. All four settings are validated when the API starts.

## Mobile flow

1. Registration sends a confirmation link and opens the check-email page.
2. The check-email page can resend the confirmation message.
3. The user opens the HTTPS confirmation link, then returns to sign in.
4. Forgot password sends a public reset link without revealing whether an
   account exists. The email also includes the one-time code so the existing
   mobile reset form remains available.
5. The web reset-password page accepts the new password and sends the email,
   reset code, and new password to the API over HTTPS.
6. The API validates the one-time Identity code and password rules. The web
   page then displays success, rejection, or temporary-unavailability status.

## Verification

Run:

    dotnet test tests/Catchlogr.Api.IntegrationTests/Catchlogr.Api.IntegrationTests.csproj
    dotnet test tests/Catchlogr.Mobile.Tests/Catchlogr.Mobile.Tests.csproj
    dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj

Integration tests replace Resend with an in-memory sender and exercise the real
Identity confirmation and reset tokens. A manual delivery smoke test should
still be performed against the verified mail.catchlogr.com domain.
