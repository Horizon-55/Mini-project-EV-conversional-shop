using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.ValueObjects;

namespace EVShop.Reviews.Domain.Entities;

public class Comment
{
    public Author Author { get; private set; } = null!;
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Comment() { } // for BSON deserialization

    public Comment(Author author, string text, DateTime? createdAt = null)
    {
        Author = author ?? throw new DomainException("Author is required for comment");
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Comment text cannot be empty");

        Text = text.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
