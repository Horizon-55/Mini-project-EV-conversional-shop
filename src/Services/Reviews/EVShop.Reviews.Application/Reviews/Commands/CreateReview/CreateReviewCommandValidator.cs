using FluentValidation;
using EVShop.Reviews.Domain.Interfaces;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository repository)
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be a positive number.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required.")
            .MaximumLength(200).WithMessage("ProductName must not exceed 200 characters.");

        RuleFor(x => x.Author)
            .NotNull().WithMessage("Author is required.");

        When(x => x.Author != null, () =>
        {
            RuleFor(x => x.Author.UserId)
                .NotEmpty().WithMessage("Author UserId is required.");

            RuleFor(x => x.Author.DisplayName)
                .NotEmpty().WithMessage("Author DisplayName is required.")
                .MaximumLength(100).WithMessage("Author DisplayName must not exceed 100 characters.");
        });

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Review text is required.")
            .MinimumLength(5).WithMessage("Review text must be at least 5 characters.")
            .MaximumLength(5000).WithMessage("Review text must not exceed 5000 characters.");

        RuleFor(x => x.DiscussionId)
            .Must(id => string.IsNullOrEmpty(id) || ObjectId.TryParse(id, out _))
            .WithMessage("DiscussionId must be a valid 24-character hex ObjectId.");

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                if (cmd.ProductId <= 0 || cmd.Author == null || string.IsNullOrWhiteSpace(cmd.Author.UserId))
                    return true; // handled by other rules
                return !await repository.ExistsAsync(cmd.ProductId, cmd.Author.UserId, ct);
            })
            .WithMessage("User has already submitted a review for this product.")
            .WithName("Author.UserId");
    }
}
