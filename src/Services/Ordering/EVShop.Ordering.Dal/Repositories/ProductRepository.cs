using Dapper;
using EVShop.Ordering.Dal.Interfaces;
using EVShop.Ordering.Domain.Entities;
using EVShop.Ordering.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace EVShop.Ordering.Dal.Repositories;

public sealed class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    public ProductRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Products")
    {
    }

    public override async Task<long> AddAsync(Product entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        const string sql = @"
            INSERT INTO dbo.Products (Id, Name, Price, StockQuantity, LastSyncedAt)
            VALUES (@Id, @Name, @Price, @StockQuantity, @LastSyncedAt);";

        var cmd = new CommandDefinition(sql, entity, Transaction, cancellationToken: ct);
        await Connection.ExecuteAsync(cmd);
        return entity.Id;
    }

    // Зменшення залишку товару в транзакції з перевіркою наявності (вимога B.4, C.2)
    public async Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        const string sql = @"
            UPDATE dbo.Products
            SET StockQuantity = StockQuantity - @Quantity
            WHERE Id = @ProductId AND StockQuantity >= @Quantity;";

        var cmd = new CommandDefinition(
            sql, 
            new { ProductId = productId, Quantity = quantity }, 
            Transaction, 
            cancellationToken: ct);

        var rowsAffected = await Connection.ExecuteAsync(cmd);

        if (rowsAffected == 0)
        {
            // Перевіряємо, чи взагалі існує такий товар
            var exists = await GetByIdAsync(productId, ct);
            if (exists == null)
                throw new NotFoundException($"Товар із кодом {productId} не знайдено в каталозі.");
            

            // Якщо товар існує, отже залишок недостатній
            throw new BusinessConflictException(
                $"Недостатньо залишку на складі для товару '{exists.Name}' (код {productId}). Поточний залишок: {exists.StockQuantity}, запитано: {quantity}.");
        }
    }

    public async Task IncreaseStockAsync(long productId, int quantity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        const string sql = @"
            UPDATE dbo.Products
            SET StockQuantity = StockQuantity + @Quantity
            WHERE Id = @ProductId;";

        var cmd = new CommandDefinition(
            sql, 
            new { ProductId = productId, Quantity = quantity }, 
            Transaction, 
            cancellationToken: ct);

        await Connection.ExecuteAsync(cmd);
    }
}
