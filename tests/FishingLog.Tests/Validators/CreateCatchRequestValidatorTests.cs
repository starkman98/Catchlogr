using FishingLog.Application.Validators;
using FishingLog.Contracts.CatchDTOs;
using FluentValidation.TestHelper;

namespace FishingLog.Tests.Validators;

public class CreateCatchRequestValidatorTests
{
    private readonly CreateCatchRequestValidator _validator = new();

    private static CreateCatchRequest ValidRequest() => new(
        Species: "Perch",
        Length: 45,
        Weight: 800,
        PhotoUrl: null,
        Note: null,
        CaughtAt: DateTime.UtcNow,
        Depth: null,
        Latitude: null,
        Longitude: null,
        Bait: null
        );

    [Fact]
    public void Should_Pass_For_Valid_Request()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Species_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(ValidRequest() with { Species = new string('x', 101) });
        result.ShouldHaveValidationErrorFor(x => x.Species);
    }

    [Fact]
    public void Should_Fail_When_Species_Is_Empty()
    {
        var result = _validator.TestValidate(ValidRequest() with { Species = "" });
        result.ShouldHaveValidationErrorFor(x => x.Species);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Should_Fail_When_Length_Is_Invalid(int length)
    {
        var result = _validator.TestValidate(ValidRequest() with { Length = length });
        result.ShouldHaveValidationErrorFor(x => x.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Should_Fail_When_Weight_Is_Invalid(int weight)
    {
        var result = _validator.TestValidate(ValidRequest() with { Weight = weight });
        result.ShouldHaveValidationErrorFor(x => x.Weight);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Should_Fail_When_Depth_Is_Zero(double depth)
    {
        var result = _validator.TestValidate(ValidRequest() with { Depth = depth });
        result.ShouldHaveValidationErrorFor(x => x.Depth);
    }

    [Theory]
    [InlineData(91)]
    [InlineData(-91)]
    [InlineData(666)]
    public void Should_Fail_When_Latitude_Is_Out_Of_Range(double latitude)
    {
        var result = _validator.TestValidate(ValidRequest() with { Latitude = latitude });
        result.ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(-90)]
    [InlineData(0)]
    public void Should_Pass_When_Latitude_Is_Valid(double latitude)
    {
        var result = _validator.TestValidate(ValidRequest() with { Latitude = latitude });
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(181)]
    [InlineData(-181)]
    [InlineData(666)]
    public void Should_Fail_When_Longitude_Is_Out_Of_Range(double longitude)
    {
        var result = _validator.TestValidate(ValidRequest() with { Longitude = longitude });
        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Theory]
    [InlineData(180)]
    [InlineData(-180)]
    [InlineData(0)]
    public void Should_Pass_When_Longitude_Is_Valid(double longitude)
    {
        var result = _validator.TestValidate(ValidRequest() with { Longitude = longitude });
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Should_Fail_When_Note_Is_Too_Long()
    {
        var result = _validator.TestValidate(ValidRequest() with { Note = new string('x', 2001) });
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Should_Fail_When_Bait_Name_Is_Empty()
    {
        var request = ValidRequest() with
        {
            Bait = new BaitDto(Name: "", Type: null, Color: null, WeightGrams: null, LengthMm: null)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("Bait.Name"); // string path for nested
    }

    [Fact]
    public void Should_Fail_When_Bait_Color_Is_Too_Long()
    {
        var request = ValidRequest() with
        {
            Bait = new BaitDto(Name: "Test color", Type: null, Color: new string('x', 51), WeightGrams: null, LengthMm: null)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("Bait.Color"); // string path for nested
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Should_Fail_When_Bait_WeightGrams_Is_Invalid(int weight)
    {
        var request = ValidRequest() with
        {
            Bait = new BaitDto(Name: "Test weight", Type: null, Color: null, WeightGrams: weight, LengthMm: null)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("Bait.WeightGrams"); // string path for nested
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Should_Fail_When_Bait_LengthMm_Is_Invalid(int length)
    {
        var request = ValidRequest() with
        {
            Bait = new BaitDto(Name: "Test length", Type: null, Color: null, WeightGrams: null, LengthMm: length)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("Bait.LengthMm"); // string path for nested
    }
}
