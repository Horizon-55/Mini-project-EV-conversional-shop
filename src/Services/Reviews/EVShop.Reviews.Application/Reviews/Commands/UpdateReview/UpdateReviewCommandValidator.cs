using FluentValidation;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Review Id is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid 24-character hex ObjectId.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Review text is required.")
            .MinimumLength(5).WithMessage("Review text must be at least 5 characters.")
            .MaximumLength(5000).WithMessage("Review text must not exceed 5000 characters.");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThanOrEqualTo(0).WithMessage("ExpectedVersion must be non-negative.");
    }
}
