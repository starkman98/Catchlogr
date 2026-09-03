using Catchlogr.Infrastructure.Email;
using Catchlogr.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
