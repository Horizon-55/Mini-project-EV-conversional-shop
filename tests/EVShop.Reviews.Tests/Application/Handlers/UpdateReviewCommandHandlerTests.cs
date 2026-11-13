using AutoMapper;
using EVShop.Reviews.Application.Mapping;
using EVShop.Reviews.Application.Reviews.Commands.UpdateReview;
using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.Interfaces;
using EVShop.Reviews.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using Xunit;

namespace EVShop.Reviews.Tests.Application.Handlers;

public class UpdateReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly UpdateReviewCommandHandler _handler;

    public UpdateReviewCommandHandlerTests()
    {
        _repositoryMock = new Mock<IReviewRepository>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReviewProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _handler = new UpdateReviewCommandHandler(_repositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var objectId = ObjectId.GenerateNewId();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(objectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review?)null);

        var command = new UpdateReviewCommand(objectId.ToString(), 5, "Updated text", 0);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenVersionMismatch_ThrowsConflictException()
    {
        // Arrange
        var objectId = ObjectId.GenerateNewId();
        var review = new Review(1, "Tesla", new Author("u1", "User"), 4, "Initial text");
        // review.Version is 0

        _repositoryMock
            .Setup(r => r.GetByIdAsync(objectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var command = new UpdateReviewCommand(objectId.ToString(), 5, "Updated text", ExpectedVersion: 2);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Optimistic concurrency violation", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenValidAndVersionMatches_UpdatesReviewAndCallsRepository()
    {
        // Arrange
        var objectId = ObjectId.GenerateNewId();
        var review = new Review(1, "Tesla", new Author("u1", "User"), 4, "Initial text");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(objectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        _repositoryMock
            .Setup(r => r.UpdateAsync(review, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateReviewCommand(objectId.ToString(), 5, "Brand new updated text", ExpectedVersion: 0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Brand new updated text", result.Text);
        _repositoryMock.Verify(r => r.UpdateAsync(review, It.IsAny<CancellationToken>()), Times.Once);
    }
}
