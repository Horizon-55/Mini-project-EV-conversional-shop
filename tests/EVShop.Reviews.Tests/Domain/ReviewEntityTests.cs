using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.ValueObjects;
using Xunit;

namespace EVShop.Reviews.Tests.Domain;

public class ReviewEntityTests
{
    private static Author CreateTestAuthor() => new("usr_1", "Test User", true);

    [Fact]
    public void Constructor_WithValidArguments_InitializesReview()
    {
        // Arrange
        var author = CreateTestAuthor();

        // Act
        var review = new Review(10, "Tesla Model 3", author, 5, "Amazing electric car!");

        // Assert
        Assert.Equal(10, review.ProductId);
        Assert.Equal("Tesla Model 3", review.ProductName);
        Assert.Equal(author, review.Author);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Amazing electric car!", review.Text);
        Assert.Equal(0, review.Version);
        Assert.Empty(review.Comments);
        Assert.True(review.CreatedAt <= DateTime.UtcNow);
        Assert.Null(review.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Constructor_WithInvalidRating_ThrowsDomainException(int invalidRating)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Review(1, "Product", CreateTestAuthor(), invalidRating, "Valid text here"));

        Assert.Contains("Rating must be between 1 and 5", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyText_ThrowsDomainException(string emptyText)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Review(1, "Product", CreateTestAuthor(), 4, emptyText));

        Assert.Contains("Review text cannot be empty", ex.Message);
    }

    [Fact]
    public void Constructor_WithNullAuthor_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Review(1, "Product", null!, 5, "Valid text"));

        Assert.Contains("Author is required", ex.Message);
    }

    [Fact]
    public void Update_WithValidArguments_UpdatesRatingAndTextAndTouchesUpdatedAt()
    {
        // Arrange
        var review = new Review(1, "Product", CreateTestAuthor(), 4, "Initial text");

        // Act
        review.Update(5, "Updated text with more detail");

        // Assert
        Assert.Equal(5, review.Rating);
        Assert.Equal("Updated text with more detail", review.Text);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void AddComment_WithValidArguments_AddsCommentAndTouchesUpdatedAt()
    {
        // Arrange
        var review = new Review(1, "Product", CreateTestAuthor(), 4, "Initial text");
        var commenter = new Author("mod_1", "Support", false);

        // Act
        review.AddComment(commenter, "Thank you for the feedback!");

        // Assert
        Assert.Single(review.Comments);
        var comment = review.Comments.First();
        Assert.Equal(commenter, comment.Author);
        Assert.Equal("Thank you for the feedback!", comment.Text);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void BumpVersion_IncrementsVersionNumber()
    {
        // Arrange
        var review = new Review(1, "Product", CreateTestAuthor(), 5, "Great car");
        Assert.Equal(0, review.Version);

        // Act
        review.BumpVersion();

        // Assert
        Assert.Equal(1, review.Version);

        review.BumpVersion();
        Assert.Equal(2, review.Version);
    }
}
