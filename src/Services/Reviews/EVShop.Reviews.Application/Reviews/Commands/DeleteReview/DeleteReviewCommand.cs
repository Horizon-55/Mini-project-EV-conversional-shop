using EVShop.Reviews.Application.Common.Interfaces;

namespace EVShop.Reviews.Application.Reviews.Commands.DeleteReview;

public record DeleteReviewCommand(string Id) : ICommand;
