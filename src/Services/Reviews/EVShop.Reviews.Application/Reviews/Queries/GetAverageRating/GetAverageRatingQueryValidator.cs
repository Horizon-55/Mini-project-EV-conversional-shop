using FluentValidation;

namespace EVShop.Reviews.Application.Reviews.Queries.GetAverageRating;

public class GetAverageRatingQueryValidator : AbstractValidator<GetAverageRatingQuery>
{
    public GetAverageRatingQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be positive.");
    }
}
