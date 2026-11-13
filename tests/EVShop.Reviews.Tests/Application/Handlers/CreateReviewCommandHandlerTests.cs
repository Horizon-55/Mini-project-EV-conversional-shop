using AutoMapper;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Application.Mapping;
using EVShop.Reviews.Application.Reviews.Commands.CreateReview;
using EVShop.Reviews.Domain.Entities;
using EVShop.Reviews.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EVShop.Reviews.Tests.Application.Handlers;

public class CreateReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly CreateReviewCommandHandler _handler;

    public CreateReviewCommandHandlerTests()
    {
        _repositoryMock = new Mock<IReviewRepository>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReviewProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _handler = new CreateReviewCommandHandler(_repositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsReviewAndReturnsDto()
    {
        // Arrange
        var command = new CreateReviewCommand(
            1,
            "BMW i4",
            new AuthorDto("usr_55", "Oleh", true),
            5,
            "Sensational performance and styling."
        );

        Review? savedReview = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .Callback<Review, CancellationToken>((r, _) => savedReview = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ProductId);
        Assert.Equal("BMW i4", result.ProductName);
        Assert.Equal(5, result.Rating);
        Assert.Equal("usr_55", result.Author.UserId);
        Assert.Equal(0, result.Version);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(savedReview);
        Assert.Equal("BMW i4", savedReview.ProductName);
    }
}
