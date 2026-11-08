using Catchlogr.Mobile.Core.Data;
using Catchlogr.Mobile.Core.Services;
using Catchlogr.Mobile.Core.Services.Authentication;
using Catchlogr.Mobile.DevHost;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Catchlogr.Mobile.Tests.DevHost;

/// <summary>Verifies temporary storage, synthetic location, and session teardown without an API.</summary>
public sealed class DevHostSessionTests
{
    /// <summary>Disposal removes SQLite and secrets even after a scenario aborts midway.</summary>
    [Fact]
    public async Task Dispose_ActivatedAccount_RemovesPrivateStorageAndTokens()
    {
        var session = DevHostSession.Create(new DevHostOptions());
        var database = session.Services.GetRequiredService<ILocalDatabase>();
        var tokens = session.Services.GetRequiredService<ITokenStore>();
        string? root = null;
        try
        {
            await database.ActivateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
            root = Directory.GetParent(database.ActiveAccountDirectory)!.Parent!.FullName;
            await tokens.SaveTokensAsync("test-access", "test-refresh", DateTimeOffset.UtcNow.AddHours(1));
            if (!OperatingSystem.IsWindows())
            {
                var mode = File.GetUnixFileMode(root);
                (mode & (UnixFileMode.GroupRead | UnixFileMode.OtherRead | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute))
                    .Should().Be((UnixFileMode)0);
            }
        }
        finally { await session.DisposeAsync(); }
        Directory.Exists(root).Should().BeFalse();
        database.ActiveUserId.Should().BeNull();
        (await tokens.GetAccessTokenAsync()).Should().BeNull();
        (await tokens.GetRefreshTokenAsync()).Should().BeNull();
        await session.DisposeAsync();
    }

    /// <summary>The optional GPS adapter supplies deterministic synthetic coordinates.</summary>
    [Fact]
    public async Task LocationAdapter_Requested_ReturnsFixedLocation()
    {
        await using var session = DevHostSession.Create(new DevHostOptions());
        var location = await session.Services.GetRequiredService<IDeviceLocationService>()
            .GetCurrentLocationAsync(TestContext.Current.CancellationToken);
        location.Should().NotBeNull();
        location.Latitude.Should().Be(60);
        location.Longitude.Should().Be(15);
    }
}
