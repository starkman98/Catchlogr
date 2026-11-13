using Catchlogr.Infrastructure.Email;
using Catchlogr.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Catchlogr.Tests.Email;

/// <summary>Tests dependency-injection registration for Identity email.</summary>
public sealed class IdentityEmailRegistrationTests
{
    /// <summary>
    /// Verifies that Identity can resolve its email sender from the root
    /// provider while endpoint routes are being mapped.
    /// </summary>
    [Fact]
    public void AddIdentityEmail_RootProvider_ResolvesSender()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:ApiKey"] = "test-key",
                ["Email:FromAddress"] = "account@mail.catchlogr.com",
                ["Email:FromName"] = "Catchlogr",
                ["Email:PublicWebBaseUrl"] =
                    "https://api.catchlogr.test"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityEmail(configuration);
        using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var action = () => serviceProvider.GetRequiredService<
            IEmailSender<ApplicationUser>>();

        action.Should().NotThrow();
    }
    /// <summary>Capture cannot accidentally replace deployed email delivery.</summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void AddIdentityEmail_CaptureOutsideE2E_Throws(string environment)
    {
        var services = new ServiceCollection();
        var configuration = CaptureConfiguration("http://localhost:8025");

        var action = () => services.AddIdentityEmail(configuration, environment);

        action.Should().Throw<InvalidOperationException>().WithMessage("*only in the E2E*");
    }

    /// <summary>Capture requires a local inbox origin and valid HTTPS action links.</summary>
    [Theory]
    [InlineData("http://localhost:8025", true)]
    [InlineData("http://mailpit:8025", true)]
    [InlineData("https://external.example", false)]
    [InlineData("http://external.example", false)]
    [InlineData("http://localhost:8025/other", false)]
    [InlineData("http://name:secret@localhost:8025", false)]
    public void AddIdentityEmail_Capture_ValidatesDestination(string destination, bool valid)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityEmail(CaptureConfiguration(destination), "E2E");
        using var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<EmailOptions>>().Value;

        if (valid)
        {
            action.Should().NotThrow();
            provider.GetRequiredService<IEmailSender<ApplicationUser>>().Should().BeOfType<IdentityEmailSender>();
        }
        else
            action.Should().Throw<OptionsValidationException>();
    }

    private static IConfiguration CaptureConfiguration(string destination)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Mailpit",
            ["Email:MailpitBaseUrl"] = destination,
            ["Email:FromAddress"] = "account@catchlogr.test",
            ["Email:FromName"] = "Catchlogr",
            ["Email:PublicWebBaseUrl"] = "https://localhost:17056"
        }).Build();

}
