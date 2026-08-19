using FishingLog.Application.Weather;
using FluentAssertions;

namespace FishingLog.Tests.Weather;

/// <summary>
/// Tests for <see cref="WmoWeatherCodeDescriptions"/>.
/// </summary>
public class WmoWeatherCodeDescriptionsTests
{
    /// <summary>
    /// Verifies that every supported WMO code maps to its expected description.
    /// </summary>
    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(1, "Mainly clear")]
    [InlineData(2, "Partly cloudy")]
    [InlineData(3, "Overcast")]
    [InlineData(45, "Fog")]
    [InlineData(48, "Depositing rime fog")]
    [InlineData(51, "Light drizzle")]
    [InlineData(53, "Moderate drizzle")]
    [InlineData(55, "Dense drizzle")]
    [InlineData(56, "Light freezing drizzle")]
    [InlineData(57, "Dense freezing drizzle")]
    [InlineData(61, "Slight rain")]
    [InlineData(63, "Moderate rain")]
    [InlineData(65, "Heavy rain")]
    [InlineData(66, "Light freezing rain")]
    [InlineData(67, "Heavy freezing rain")]
    [InlineData(71, "Slight snowfall")]
    [InlineData(73, "Moderate snowfall")]
    [InlineData(75, "Heavy snowfall")]
    [InlineData(77, "Snow grains")]
    [InlineData(80, "Slight rain showers")]
    [InlineData(81, "Moderate rain showers")]
    [InlineData(82, "Violent rain showers")]
    [InlineData(85, "Slight snow showers")]
    [InlineData(86, "Heavy snow showers")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(96, "Thunderstorm with slight hail")]
    [InlineData(99, "Thunderstorm with heavy hail")]
    public void GetDescription_Should_ReturnExpectedDescription(
        int weatherCode,
        string expectedDescription)
    {
        var result = WmoWeatherCodeDescriptions.GetDescription(weatherCode);

        result.Should().Be(expectedDescription);
    }

    /// <summary>
    /// Verifies that missing weather data has a user-friendly description.
    /// </summary>
    [Fact]
    public void GetDescription_Should_ReturnUnavailable_WhenCodeIsNull()
    {
        var result = WmoWeatherCodeDescriptions.GetDescription(null);

        result.Should().Be("Weather unavailable");
    }

    /// <summary>
    /// Verifies that an unsupported provider code does not cause an error.
    /// </summary>
    [Fact]
    public void GetDescription_Should_ReturnUnknown_WhenCodeIsUnsupported()
    {
        var result = WmoWeatherCodeDescriptions.GetDescription(1234);

        result.Should().Be("Unknown weather condition");
    }
}
