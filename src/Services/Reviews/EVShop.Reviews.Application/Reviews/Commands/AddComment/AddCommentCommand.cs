using EVShop.Reviews.Application.Common.Interfaces;
using EVShop.Reviews.Application.DTOs;

namespace EVShop.Reviews.Application.Reviews.Commands.AddComment;

public record AddCommentCommand(
    string ReviewId,
    AuthorDto Author,
    string Text
) : ICommand<CommentDto>;
