using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.ValueObjects;
using Xunit;

namespace EVShop.Reviews.Tests.Domain;

public class AuthorValueObjectTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesProperties()
    {
        var author = new Author("usr_123", "Ivan Petrenko", true);

        Assert.Equal("usr_123", author.UserId);
        Assert.Equal("Ivan Petrenko", author.DisplayName);
        Assert.True(author.IsVerifiedBuyer);
    }

    [Theory]
    [InlineData("", "Valid Name")]
    [InlineData("   ", "Valid Name")]
    [InlineData("usr_1", "")]
    [InlineData("usr_1", "   ")]
    public void Constructor_WithInvalidArguments_ThrowsDomainException(string userId, string displayName)
    {
        Assert.Throws<DomainException>(() => new Author(userId, displayName));
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var author1 = new Author("usr_1", "Alex", true);
        var author2 = new Author("usr_1", "Alex", true);

        Assert.Equal(author1, author2);
        Assert.True(author1 == author2);
        Assert.Equal(author1.GetHashCode(), author2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        var author1 = new Author("usr_1", "Alex", true);
        var author2 = new Author("usr_2", "Alex", true);

        Assert.NotEqual(author1, author2);
        Assert.True(author1 != author2);
    }
}
