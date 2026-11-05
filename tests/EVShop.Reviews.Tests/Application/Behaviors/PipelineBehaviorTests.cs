using EVShop.Reviews.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DomainValidationException = EVShop.Reviews.Domain.Exceptions.ValidationException;

namespace EVShop.Reviews.Tests.Application.Behaviors;

public class PipelineBehaviorTests
{
    public record TestCommand(string Name) : IRequest<string>;

    [Fact]
    public async Task ValidationBehavior_WhenValidationFails_ThrowsValidationExceptionWithoutCallingNext()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name is required")
            }));

        var behavior = new ValidationBehavior<TestCommand, string>(new[] { validatorMock.Object });

        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            behavior.Handle(new TestCommand(""), next, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.True(ex.Errors.ContainsKey("Name"));
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidationPasses_CallsNextDelegate()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestCommand, string>(new[] { validatorMock.Object });

        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(new TestCommand("Valid"), next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task PerformanceBehavior_ExecutesNextDelegateSuccessfully()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceBehavior<TestCommand, string>>>();
        var behavior = new PerformanceBehavior<TestCommand, string>(loggerMock.Object);

        RequestHandlerDelegate<string> next = _ => Task.FromResult("Processed");

        // Act
        var result = await behavior.Handle(new TestCommand("Test"), next, CancellationToken.None);

        // Assert
        Assert.Equal("Processed", result);
    }
}
