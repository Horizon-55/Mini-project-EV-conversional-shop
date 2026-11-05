using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.DTOs;

namespace EVShop.Reviews.Application.Reviews.Commands.UpdateReview;

public record UpdateReviewCommand(
    string Id,
    int Rating,
    string Text,
    int ExpectedVersion
) : ICommand<ReviewDto>;
