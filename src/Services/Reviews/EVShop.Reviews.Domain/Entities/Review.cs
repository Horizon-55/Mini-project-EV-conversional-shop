using EVShop.Reviews.Domain.Common;
using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.ValueObjects;
using MongoDB.Bson;

namespace EVShop.Reviews.Domain.Entities;

public class Review : BaseEntity
{
    private readonly List<Comment> _comments = new();

    public long ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Author Author { get; private set; } = null!;
    public int Rating { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public ObjectId? DiscussionId { get; private set; }

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private Review() { } // for BSON deserialization

    public Review(long productId, string productName, Author author, int rating, string text, ObjectId? discussionId = null)
    {
        if (productId <= 0)
            throw new DomainException("ProductId must be positive");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("ProductName is required");

        Author = author ?? throw new DomainException("Author is required");

        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5");

        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Review text cannot be empty");

        ProductId = productId;
        ProductName = productName.Trim();
        Rating = rating;
        Text = text.Trim();
        DiscussionId = discussionId;
    }

    public void Update(int rating, string text)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5");

        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Review text cannot be empty");

        Rating = rating;
        Text = text.Trim();
        Touch();
    }

    public void AddComment(Author author, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Comment text is required");

        _comments.Add(new Comment(author, text));
        Touch();
    }

    public void AttachDiscussion(ObjectId discussionId)
    {
        DiscussionId = discussionId;
        Touch();
    }
}
