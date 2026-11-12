using Dapper;
using EVShop.Ordering.Dal.Interfaces;
using Microsoft.Data.SqlClient;

namespace EVShop.Ordering.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected readonly string TableName;

    protected BaseDapperRepository(
        SqlConnection connection,
        Func<SqlTransaction?> currentTransaction,
        string tableName)
        : base(connection, currentTransaction)
    {
        // Константа з коду, безпечна проти SQL-ін'єкцій
        TableName = tableName;
    }

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct);
        return await Connection.QuerySingleOrDefaultAsync<T>(cmd);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"SELECT * FROM {TableName}";
        var cmd = new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct);
        var result = await Connection.QueryAsync<T>(cmd);
        return result.AsList();
    }

    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);

    public virtual async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        // За замовчуванням виконуємо м'яке видалення (soft delete)
        var sql = $"UPDATE {TableName} SET IsDeleted = 1 WHERE Id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct);
        var rows = await Connection.ExecuteAsync(cmd);
        return rows > 0;
    }
}
