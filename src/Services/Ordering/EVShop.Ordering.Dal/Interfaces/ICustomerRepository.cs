using EVShop.Ordering.Domain.Entities;

namespace EVShop.Ordering.Dal.Interfaces;

// Репозиторій клієнтів на чистому ADO.NET
// Свідомо НЕ успадковує IGenericRepository, демонструє пряму низькорівневу роботу з SqlClient
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(Customer customer, CancellationToken ct = default);
}
