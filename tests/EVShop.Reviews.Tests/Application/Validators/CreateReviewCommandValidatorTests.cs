using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Application.Reviews.Commands.CreateReview;
using EVShop.Reviews.Domain.Interfaces;
using Moq;
using Xunit;

namespace EVShop.Reviews.Tests.Application.Validators;

public class CreateReviewCommandValidatorTests
{
    private readonly Mock<IReviewRepository> _repositoryMock;
    private readonly CreateReviewCommandValidator _validator;

    public CreateReviewCommandValidatorTests()
    {
        _repositoryMock = new Mock<IReviewRepository>();
        _validator = new CreateReviewCommandValidator(_repositoryMock.Object);
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidAndNoReviewExists_ReturnsValid()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsAsync(1, "usr_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateReviewCommand(
            1,
            "Nissan Leaf",
            new AuthorDto("usr_1", "Test User", true),
            5,
            "Great electric car for daily commute!"
        );

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Validate_WhenRatingOutOfRange_ReturnsValidationError(int invalidRating)
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateReviewCommand(
            1,
            "Nissan Leaf",
            new AuthorDto("usr_1", "Test User", true),
            invalidRating,
            "Great electric car!"
        );

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Rating");
    }

    [Fact]
    public async Task Validate_WhenUserAlreadySubmittedReview_ReturnsValidationError()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsAsync(1, "usr_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateReviewCommand(
            1,
            "Nissan Leaf",
            new AuthorDto("usr_1", "Test User", true),
            5,
            "Trying to submit duplicate review"
        );

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("already submitted"));
    }

    [Fact]
    public async Task Validate_WhenTextTooShort_ReturnsValidationError()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateReviewCommand(
            1,
            "Nissan Leaf",
            new AuthorDto("usr_1", "Test User", true),
            5,
            "Bad" // only 3 chars, min length is 5
        );

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Text");
    }
}
