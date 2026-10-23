using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catchlogr.Contracts.AuthenticationDTOs;
using Catchlogr.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Catchlogr.Api.IntegrationTests;

/// <summary>Exercises production password validation through real Identity endpoints.</summary>
public sealed class PasswordPolicyTests
{
    private const string Email = "angler@example.com";
    private const string Password = "reeds beside quiet water";

    /// <summary>Checks rejection across registration, reset, and authenticated change.</summary>
    [Theory]
    [InlineData("register", "short", PasswordRequirements.InvalidLength)]
    [InlineData("register", "passwordpassword", PasswordRequirements.Blocklisted)]
    [InlineData("register", "catchlogr123456789!", PasswordRequirements.Predictable)]
    [InlineData("reset", "short", PasswordRequirements.InvalidLength)]
    [InlineData("reset", "PASSWORDPASSWORD", PasswordRequirements.Blocklisted)]
    [InlineData("reset", "angler@example.com", PasswordRequirements.Predictable)]
    [InlineData("change", "short", PasswordRequirements.InvalidLength)]
    [InlineData("change", "passwordpassword", PasswordRequirements.Blocklisted)]
    [InlineData("change", "catchlogr123456789!", PasswordRequirements.Predictable)]
    public async Task SetPassword_InvalidPassword_RejectsWithoutChangingAccount(
        string operation, string password, string expectedCode)
    {
        using var factory = new CatchlogrApiFactory();
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();
        HttpResponseMessage response;
        if (operation == "register")
        {
            response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(Email, password),
                TestContext.Current.CancellationToken);
            factory.EmailSender.ConfirmationLinks.Should().BeEmpty();
            using var scope = factory.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            (await manager.FindByEmailAsync(Email)).Should().BeNull();
        }
        else
        {
            await RegisterAsync(client);
            if (operation == "reset")
            {
                // Reset emails are issued only for confirmed accounts.
                using var scope = factory.Services.CreateScope();
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = (await manager.FindByEmailAsync(Email))!;
                var confirmation = await manager.GenerateEmailConfirmationTokenAsync(user);
                (await manager.ConfirmEmailAsync(user, confirmation)).Succeeded.Should().BeTrue();
                using var forgot = await client.PostAsJsonAsync("/api/auth/forgotPassword",
                    new ForgotPasswordRequest(Email), TestContext.Current.CancellationToken);
                forgot.EnsureSuccessStatusCode();
                var code = WebUtility.HtmlDecode(factory.EmailSender.PasswordResetCodes.Single());
                response = await client.PostAsJsonAsync("/api/auth/resetPassword",
                    new ResetPasswordRequest(Email, code, password), TestContext.Current.CancellationToken);
            }
            else
            {
                var tokens = await LoginAsync(client, Password);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
                response = await client.PostAsJsonAsync("/api/auth/manage/info",
                    new { oldPassword = Password, newPassword = password }, TestContext.Current.CancellationToken);
            }
        }

        using (response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            PasswordValidationErrors.Read(json).Should().Contain(expectedCode);
            json.Should().NotContain(password);
        }

        if (operation != "register")
            (await LoginAsync(client, Password)).AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Accepts both length boundaries and preserves spaces and non-BMP characters.</summary>
    [Theory]
    [InlineData("ascii", 15)]
    [InlineData("ascii", 128)]
    [InlineData("unicode", 15)]
    [InlineData("spaces", 15)]
    public async Task Register_ValidPassword_PreservesSecretForLogin(string kind, int length)
    {
        using var factory = new CatchlogrApiFactory();
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();
        var password = kind switch
        {
            "unicode" => string.Concat(Enumerable.Repeat("🐟", length)),
            "spaces" => "  reeds beside quiet water  ",
            _ => string.Concat(Enumerable.Repeat("reedsalongshore", 9))[..length]
        };

        await RegisterAsync(client, password);
        (await LoginAsync(client, password)).AccessToken.Should().NotBeNullOrWhiteSpace();
        if (kind == "spaces")
        {
            using var trimmedLogin = await client.PostAsJsonAsync("/api/auth/login?useCookies=false",
                new LoginRequest(Email, password.Trim()), TestContext.Current.CancellationToken);
            trimmedLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>Rejects overlong passwords instead of truncating them.</summary>
    [Theory]
    [InlineData(14)]
    [InlineData(129)]
    public async Task Register_OutsideLengthBoundary_Rejects(int length)
    {
        using var factory = new CatchlogrApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(Email, new string('z', length)), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        PasswordValidationErrors.Read(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Equal(PasswordRequirements.InvalidLength);
    }

    /// <summary>Changing validation rules does not prevent existing shorter passwords from signing in.</summary>
    [Fact]
    public async Task Login_LegacyPassword_StillWorks()
    {
        using var factory = new CatchlogrApiFactory();
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = Email, Email = Email, EmailConfirmed = true };
        user.PasswordHash = manager.PasswordHasher.HashPassword(user, "Password1!");
        (await manager.CreateAsync(user)).Succeeded.Should().BeTrue();
        using var client = factory.CreateClient();

        (await LoginAsync(client, "Password1!")).AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Verifies successful password changes and refresh-token invalidation.</summary>
    [Fact]
    public async Task ChangePassword_ValidPassphrase_ChangesSecretAndInvalidatesRefresh()
    {
        using var factory = new CatchlogrApiFactory();
        await factory.InitializeAsync(TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();
        await RegisterAsync(client);
        var originalTokens = await LoginAsync(client, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", originalTokens.AccessToken);
        const string newPassword = "another quiet water passphrase";

        using var change = await client.PostAsJsonAsync("/api/auth/manage/info",
            new { oldPassword = Password, newPassword = newPassword }, TestContext.Current.CancellationToken);
        change.EnsureSuccessStatusCode();
        (await LoginAsync(client, newPassword)).AccessToken.Should().NotBeNullOrWhiteSpace();
        using var oldLogin = await client.PostAsJsonAsync("/api/auth/login?useCookies=false",
            new LoginRequest(Email, Password), TestContext.Current.CancellationToken);
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var refresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(originalTokens.RefreshToken), TestContext.Current.CancellationToken);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // Identity bearer access tokens retain their existing lifetime after a password change.
        using var currentUser = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        currentUser.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task RegisterAsync(HttpClient client, string password = Password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(Email, password),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<AccessTokenResponse> LoginAsync(HttpClient client, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login?useCookies=false", new LoginRequest(Email, password),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccessTokenResponse>(TestContext.Current.CancellationToken))!;
    }
}
