using FishingLog.Contracts.FishingTripDTOs;
using FluentValidation;

namespace FishingLog.Application.Validators;

/// <summary>
/// Validates requests that create fishing trips.
/// </summary>
public sealed class CreateFishingTripRequestValidator : AbstractValidator<CreateFishingTripRequest>
{
    /// <summary>
    /// Initializes the validation rules for fishing-trip creation.
    /// </summary>
    public CreateFishingTripRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must be 200 characters or fewer.");

        RuleFor(x => x.LocationName)
            .MaximumLength(500).WithMessage("LocationName ,ust be 500 characters or fewer.")
            .When(x => x.LocationName is not null);

        RuleFor(x => x.WeatherDescription)
            .MaximumLength(500).WithMessage("WeatherDescription must be 500 characters or fewer.")
            .When(x => x.WeatherDescription is not null);

        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("Note must be 2000 characters or fewer.")
            .When(x => x.Note is not null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude is not null);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude is not null);

        RuleFor(x => x.Latitude)
            .NotNull().WithMessage("Latitude is required when longitude is provided.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.Longitude)
            .NotNull().WithMessage("Longitude is required when latitude is provided.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.WaterTemp)
            .InclusiveBetween(0, 100).WithMessage("WaterTemp must be between 0 and 100 °C.")
            .When(x => x.WaterTemp is not null);

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be after StartTime.")
            .When(x => x.EndTime is not null);
    }
}
