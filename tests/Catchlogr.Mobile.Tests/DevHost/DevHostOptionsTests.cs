using Catchlogr.Mobile.DevHost;
using FluentAssertions;

namespace Catchlogr.Mobile.Tests.DevHost;

/// <summary>Verifies local endpoint restrictions and the non-secret CLI contract.</summary>
public sealed class DevHostOptionsTests
{
    /// <summary>Only loopback HTTP and HTTPS origins are accepted.</summary>
    [Theory]
    [InlineData("http://127.0.0.1:5001")]
    [InlineData("http://[::1]:5001/")]
    [InlineData("https://localhost:7160/")]
    public void Parse_LoopbackOrigin_Accepts(string origin)
    {
        var options = DevHostOptions.Parse(["--api", origin, "--name", "Smoke trip", "--with-location"]);
        options.ApiBaseUri.Should().Be(new Uri(origin));
        options.TripName.Should().Be("Smoke trip");
        options.WithLocation.Should().BeTrue();
    }

    /// <summary>Remote addresses, embedded credentials, and ambiguous API paths fail before login.</summary>
    [Theory]
    [InlineData("https://api.catchlogr.com")]
    [InlineData("http://192.168.1.10:5001")]
    [InlineData("http://localhost.example.com")]
    [InlineData("http://name:secret@localhost:5001")]
    [InlineData("http://localhost:5001/api")]
    [InlineData("http://localhost:5001/?token=secret")]
    [InlineData("http://localhost:5001/#fragment")]
    [InlineData("ftp://localhost")]
    [InlineData("invalid")]
    public void Parse_UnsupportedOrigin_Rejects(string origin)
    {
        var action = () => DevHostOptions.Parse(["--api", origin]);
        action.Should().Throw<ArgumentException>().Which.Message.Should().NotContain("secret");
    }

    /// <summary>The CLI never accepts credential arguments or silently ignores invalid options.</summary>
    [Theory]
    [InlineData("--password")]
    [InlineData("--email")]
    [InlineData("--unknown")]
    [InlineData("--api")]
    [InlineData("--name")]
    public void Parse_UnknownOrIncompleteOption_Rejects(string option)
    {
        var action = () => DevHostOptions.Parse([option]);
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>A repeated option fails instead of silently changing the selected backend.</summary>
    [Fact]
    public void Parse_RepeatedApi_Rejects()
    {
        var action = () => DevHostOptions.Parse(["--api", "http://localhost:5001", "--api", "http://localhost:5002"]);
        action.Should().Throw<ArgumentException>();
    }
}
