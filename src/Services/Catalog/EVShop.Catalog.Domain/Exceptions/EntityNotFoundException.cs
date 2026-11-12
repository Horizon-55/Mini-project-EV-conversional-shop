namespace EVShop.Catalog.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string message) : base(message) { }
}
