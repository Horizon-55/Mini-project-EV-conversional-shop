using FluentValidation;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Queries.GetReviewById;

public class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Review Id is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid 24-character hex ObjectId.");
    }
}
