using EVShop.Reviews.Domain.Common;
using EVShop.Reviews.Domain.Exceptions;

namespace EVShop.Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public string UserId { get; }
    public string DisplayName { get; }
    public bool IsVerifiedBuyer { get; }

    public Author(string userId, string displayName, bool isVerifiedBuyer = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId is required for Author");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("DisplayName is required");

        UserId = userId.Trim();
        DisplayName = displayName.Trim();
        IsVerifiedBuyer = isVerifiedBuyer;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return DisplayName;
        yield return IsVerifiedBuyer;
    }
}
