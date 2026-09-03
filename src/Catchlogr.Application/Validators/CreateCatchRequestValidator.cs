using Catchlogr.Contracts.CatchDTOs;
using FluentValidation;

namespace Catchlogr.Application.Validators;

public sealed class CreateCatchRequestValidator : AbstractValidator<CreateCatchRequest>
{
    public CreateCatchRequestValidator()
    {
        RuleFor(c => c.Species)
            .NotEmpty().WithMessage("Species is required.")
            .MaximumLength(100).WithMessage("Species must be 100 characters or fewer.");

        RuleFor(c => c.CaughtAt)
            .NotEmpty().WithMessage("CaughtAt is required.");

        RuleFor(c => c.Length)
            .GreaterThan(0).WithMessage("Length must be greater than 0.")
            .When(c => c.Length is not null);

        RuleFor(c => c.Weight)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .When(c => c.Weight is not null);

        RuleFor(c => c.Depth)
            .GreaterThan(0).WithMessage("Depth cannot be negative.")
            .When(c => c.Depth is not null);

        RuleFor(c => c.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(c => c.Latitude is not null);

        RuleFor(c => c.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(c => c.Longitude is not null);

        RuleFor(c => c.Note)
            .MaximumLength(2000).WithMessage("Note must be 2000 characters or fewer.")
            .When(c => c.Note is not null);

        RuleFor(c => c.Bait!).ChildRules(bait =>
        {
            bait.RuleFor(b => b.Name)
                .NotEmpty().WithMessage("Bait name is required.")
                .MaximumLength(100).WithMessage("Bait name must be 100 characters or fewer.");

            bait.RuleFor(b => b.Color)
                .MaximumLength(50).WithMessage("Bait color must be 50 characters or fewer.")
                .When(b => b.Color is not null);

            bait.RuleFor(b => b.WeightGrams)
                .GreaterThan(0).WithMessage("Bait weight must be greater than 0.")
                .When(b => b.WeightGrams is not null);

            bait.RuleFor(b => b.LengthMm)
                .GreaterThan(0).WithMessage("Bait length must be greater than 0.")
                .When(b => b.LengthMm is not null);
        }).When(c => c.Bait is not null);
    }
}
