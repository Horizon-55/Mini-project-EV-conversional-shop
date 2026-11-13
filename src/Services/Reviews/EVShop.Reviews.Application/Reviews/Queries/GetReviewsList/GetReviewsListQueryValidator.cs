using FluentValidation;

namespace EVShop.Reviews.Application.Reviews.Queries.GetReviewsList;

public class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        When(x => x.ProductId.HasValue, () =>
        {
            RuleFor(x => x.ProductId!.Value)
                .GreaterThan(0).WithMessage("ProductId must be positive.");
        });

        When(x => x.Rating.HasValue, () =>
        {
            RuleFor(x => x.Rating!.Value)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        });
    }
}
