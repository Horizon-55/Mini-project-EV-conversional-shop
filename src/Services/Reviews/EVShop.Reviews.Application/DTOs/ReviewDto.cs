namespace EVShop.Reviews.Application.DTOs;

public record AuthorDto(
    string UserId,
    string DisplayName,
    bool IsVerifiedBuyer
);

public record CommentDto(
    AuthorDto Author,
    string Text,
    DateTime CreatedAt
);

public record ReviewDto(
    string Id,
    long ProductId,
    string ProductName,
    AuthorDto Author,
    int Rating,
    string Text,
    string? DiscussionId,
    IReadOnlyCollection<CommentDto> Comments,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version
);

public record CreateReviewDto(
    long ProductId,
    string ProductName,
    AuthorDto Author,
    int Rating,
    string Text,
    string? DiscussionId = null
);

public record UpdateReviewDto(
    int Rating,
    string Text,
    int ExpectedVersion
);

public record AddCommentDto(
    AuthorDto Author,
    string Text
);
