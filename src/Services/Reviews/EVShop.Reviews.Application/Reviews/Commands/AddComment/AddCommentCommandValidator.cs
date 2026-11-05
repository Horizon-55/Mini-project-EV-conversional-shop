using FluentValidation;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.AddComment;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("ReviewId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("ReviewId must be a valid 24-character hex ObjectId.");

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

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Comment text is required.")
            .MaximumLength(2000).WithMessage("Comment text must not exceed 2000 characters.");
    }
}
