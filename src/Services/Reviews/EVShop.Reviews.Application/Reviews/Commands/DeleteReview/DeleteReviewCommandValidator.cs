using FluentValidation;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Review Id is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid 24-character hex ObjectId.");
    }
}
