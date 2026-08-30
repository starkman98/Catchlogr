using Catchlogr.Mobile.Configuration;
using FluentAssertions;

namespace Catchlogr.Mobile.Tests.Configuration;

/// <summary>
/// Tests environment-aware platform API URL resolution.
/// </summary>
public sealed class PlatformApiUrlTests
{
    /// <summary>Verifies that non-Local URLs are never rewritten.</summary>
    [Theory]
    [InlineData(BackendEnvironment.Development, "https://dev-api.catchlogr.com")]
    [InlineData(BackendEnvironment.Production, "https://api.catchlogr.com")]
    public void Resolve_NonLocalEnvironment_ReturnsConfiguredUrl(
        BackendEnvironment environment,
        string configuredUrl)
    {
        var result = PlatformApiUrl.Resolve(
            configuredUrl,
            environment,
            DevicePlatform.Android,
            DeviceType.Virtual);

        result.Should().Be(configuredUrl);
    }

    /// <summary>Verifies Android emulator access to the host HTTP endpoint.</summary>
    [Fact]
    public void Resolve_LocalAndroidEmulator_ReturnsHostAlias()
    {
        var result = PlatformApiUrl.Resolve(
            "https://localhost:7160",
            BackendEnvironment.Local,
            DevicePlatform.Android,
            DeviceType.Virtual);

        result.Should().Be("http://10.0.2.2:5001");
    }

    /// <summary>Verifies Windows access to the local HTTPS endpoint.</summary>
    [Fact]
    public void Resolve_LocalWindows_ReturnsLocalHttpsEndpoint()
    {
        var result = PlatformApiUrl.Resolve(
            "http://192.168.1.10:5001",
            BackendEnvironment.Local,
            DevicePlatform.WinUI,
            DeviceType.Physical);

        result.Should().Be("https://localhost:7160");
    }

    /// <summary>Verifies physical devices keep a configured LAN endpoint.</summary>
    [Fact]
    public void Resolve_LocalPhysicalDevice_ReturnsConfiguredUrl()
    {
        const string configuredUrl = "http://192.168.1.10:5001";

        var result = PlatformApiUrl.Resolve(
            configuredUrl,
            BackendEnvironment.Local,
            DevicePlatform.Android,
            DeviceType.Physical);

        result.Should().Be(configuredUrl);
    }
}
