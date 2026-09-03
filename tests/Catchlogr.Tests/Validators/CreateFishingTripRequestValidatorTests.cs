using Catchlogr.Application.Validators;
using Catchlogr.Contracts.FishingTripDTOs;
using FluentValidation.TestHelper;

namespace Catchlogr.Tests.Validators;

public class CreateFishingTripRequestValidatorTests
{
    private readonly CreateFishingTripRequestValidator _validator = new();

    private static CreateFishingTripRequest ValidRequest() => new(
        Name: "Morning perch session",
        LocationName: "Lake Vänern",
        WaterTemp: 14.6,
        WeatherDescription: "Sunny",
        Latitude: 58.9,
        Longitude: 13.5,
        StartTime: new DateTime(2025, 6, 1, 6, 0, 0),
        EndTime: new DateTime(2025, 6, 1, 12, 0, 0),
        Note: "This was a very calm day att the lake, the perch wasn't active though.");

    [Fact]
    public void Should_Pass_For_Valid_Request()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var result = _validator.TestValidate(ValidRequest() with { Name = "" });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_When_EndTime_Is_Before_StartTime()
    {
        var request = ValidRequest() with
        {
            StartTime = new DateTime(2025, 6, 1, 12, 0, 0),
            EndTime = new DateTime(2025, 6, 1, 6, 0, 0)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndTime);
    }

    [Fact]
    public void Should_Fail_When_Latitude_Out_Of_Range()
    {
        var result = _validator.TestValidate(ValidRequest() with { Latitude = 999 });
        result.ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public void Should_Fail_When_Longitude_Out_Of_Range()
    {
        var result = _validator.TestValidate(ValidRequest() with { Longitude = -999 });
        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Should_Fail_When_Only_Latitude_Is_Provided()
    {
        var result = _validator.TestValidate(ValidRequest() with
        {
            Latitude = 58.9,
            Longitude = null
        });

        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Should_Fail_When_Only_Longitude_Is_Provided()
    {
        var result = _validator.TestValidate(ValidRequest() with
        {
            Latitude = null,
            Longitude = 13.5
        });

        result.ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public void Should_Pass_When_Both_Coordinates_Are_Null()
    {
        var result = _validator.TestValidate(ValidRequest() with
        {
            Latitude = null,
            Longitude = null
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Name_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(ValidRequest() with { Name = new string('x', 201) });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Pass_When_EndTime_Is_Null()
    {
        var result = _validator.TestValidate(ValidRequest() with { EndTime = null });
        result.ShouldNotHaveAnyValidationErrors();
    }
}
