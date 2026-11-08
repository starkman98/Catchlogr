using Catchlogr.Mobile.Core.Configuration;
using FluentAssertions;

namespace Catchlogr.Mobile.Tests.Configuration;

/// <summary>Verifies host-owned embedded configuration and environment validation.</summary>
public sealed class AppSettingsTests
{
    /// <summary>Each build environment selects its existing mobile resource from the host assembly.</summary>
    [Theory]
    [InlineData(BackendEnvironment.Local, "catchlogr_local.db3")]
    [InlineData(BackendEnvironment.Development, "catchlogr_dev.db3")]
    [InlineData(BackendEnvironment.Production, "catchlogr.db3")]
    public void Load_HostAssembly_SelectsEnvironmentResource(BackendEnvironment environment, string fileName)
    {
        var settings = AppSettings.Load(typeof(AppSettingsTests).Assembly, environment);

        settings.BackendEnvironment.Should().Be(environment);
        settings.Database.FileName.Should().Be(fileName);
        settings.Database.RootDirectory.Should().BeNull();
    }

    /// <summary>A missing resource fails explicitly instead of silently loading another backend.</summary>
    [Fact]
    public void Load_AssemblyWithoutResources_FailsExplicitly()
    {
        var action = () => AppSettings.Load(typeof(AppSettings).Assembly, BackendEnvironment.Local);

        action.Should().Throw<InvalidOperationException>().WithMessage("Could not find embedded resource: *Local.json");
    }
}
