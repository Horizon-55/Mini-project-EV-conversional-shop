using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.DTOs;

namespace EVShop.Reviews.Application.Reviews.Commands.CreateReview;

public record CreateReviewCommand(
    long ProductId,
    string ProductName,
    AuthorDto Author,
    int Rating,
    string Text,
    string? DiscussionId = null
) : ICommand<ReviewDto>;
