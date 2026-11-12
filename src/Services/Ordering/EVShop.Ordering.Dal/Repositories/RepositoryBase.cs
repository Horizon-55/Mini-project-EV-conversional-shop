using System.Data;
using Microsoft.Data.SqlClient;

namespace EVShop.Ordering.Dal.Repositories;

public abstract class RepositoryBase
{
    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _currentTransaction;

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    // Поточна активна транзакція Unit of Work (або null, якщо транзакцію не розпочато)
    protected SqlTransaction? Transaction => _currentTransaction();

    protected async Task EnsureOpenAsync(CancellationToken ct = default)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(ct);
        }
    }
}
