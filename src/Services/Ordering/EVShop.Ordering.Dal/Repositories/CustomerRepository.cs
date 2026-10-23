using System.Data;
using EVShop.Ordering.Dal.Interfaces;
using EVShop.Ordering.Domain.Entities;
using EVShop.Ordering.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace EVShop.Ordering.Dal.Repositories;

// Репозиторій клієнтів на чистому ADO.NET
public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction)
    {
    }

    public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Email, FullName, PhoneNumber, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
            FROM dbo.Customers 
            WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return MapCustomer(reader);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Email, FullName, PhoneNumber, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
            FROM dbo.Customers 
            WHERE Email = @Email AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = email;

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return MapCustomer(reader);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Email, FullName, PhoneNumber, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
            FROM dbo.Customers 
            WHERE IsDeleted = 0
            ORDER BY Id;";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var list = new List<Customer>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapCustomer(reader));
        }

        return list;
    }

    public async Task<long> AddAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Customers (Email, FullName, PhoneNumber, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@Email, @FullName, @PhoneNumber, @CreatedBy);";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = customer.Email;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = customer.FullName;
        command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 25).Value = (object?)customer.PhoneNumber ?? DBNull.Value;
        command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = customer.CreatedBy;

        try
        {
            var result = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(result);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new BusinessConflictException($"Клієнт із поштою '{customer.Email}' вже зареєстрований.");
        }
    }

    private static Customer MapCustomer(SqlDataReader reader)
    {
        return new Customer
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetString(reader.GetOrdinal("UpdatedBy")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
        };
    }
}
