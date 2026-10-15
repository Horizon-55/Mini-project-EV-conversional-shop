using System.Data;
using Dapper;
using EVShop.Ordering.Dal.Interfaces;
using EVShop.Ordering.Domain.Entities;
using EVShop.Ordering.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace EVShop.Ordering.Dal.Repositories;

public sealed class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    public OrderRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Orders")
    {
    }

    public override async Task<long> AddAsync(Order entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@CustomerId", entity.CustomerId);
        parameters.Add("@ShippingAddress", entity.Details?.ShippingAddress ?? "Самовивіз");
        parameters.Add("@ContactPhone", entity.Details?.ContactPhone ?? "+380000000000");
        parameters.Add("@TargetVehicleModel", entity.Details?.TargetVehicleModel ?? "Не вказано");
        parameters.Add("@ConversionType", entity.Details?.ConversionType ?? "DIY_Kit");
        parameters.Add("@InstallationNotes", entity.Details?.InstallationNotes);
        parameters.Add("@CreatedBy", entity.CreatedBy);
        parameters.Add("@NewOrderId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        try
        {
            var cmd = new CommandDefinition(
                "dbo.usp_CreateOrder",
                parameters,
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            await Connection.ExecuteAsync(cmd);
            var orderId = parameters.Get<long>("@NewOrderId");
            entity.Id = orderId;
            return orderId;
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex, entity.Id);
        }
    }

    // Dapper Multi-Mapping: вибірка замовлення разом із деталями та списком позицій в один запит
    public async Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        const string sql = @"
            SELECT 
                o.Id, o.CustomerId, o.Status, o.OrderDate, o.TotalAmount, o.CreatedAt, o.CreatedBy, o.UpdatedAt, o.UpdatedBy, o.IsDeleted, o.RowVersion,
                d.OrderId, d.ShippingAddress, d.ContactPhone, d.TargetVehicleModel, d.ConversionType, d.InstallationNotes,
                i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Discount
            FROM dbo.Orders o
            LEFT JOIN dbo.OrderDetails d ON d.OrderId = o.Id
            LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
            WHERE o.Id = @OrderId AND o.IsDeleted = 0";

        var orderDictionary = new Dictionary<long, Order>();

        var cmd = new CommandDefinition(
            sql, 
            new { OrderId = orderId }, 
            Transaction, 
            cancellationToken: ct);

        await Connection.QueryAsync<Order, OrderDetails?, OrderItem?, Order>(
            cmd,
            (order, details, item) =>
            {
                if (!orderDictionary.TryGetValue(order.Id, out var currentOrder))
                {
                    currentOrder = order;
                    currentOrder.Details = details;
                    currentOrder.Items = new List<OrderItem>();
                    orderDictionary.Add(currentOrder.Id, currentOrder);
                }

                if (item != null && item.ProductId > 0 && !currentOrder.Items.Any(x => x.ProductId == item.ProductId))
                {
                    currentOrder.Items.Add(item);
                }

                return currentOrder;
            },
            splitOn: "OrderId,OrderId");

        return orderDictionary.Values.FirstOrDefault();
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(long customerId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        const string sql = @"
            SELECT o.Id, o.CustomerId, o.Status, o.OrderDate, o.TotalAmount, o.CreatedAt, o.CreatedBy, o.UpdatedAt, o.UpdatedBy, o.IsDeleted, o.RowVersion,
                   d.OrderId, d.ShippingAddress, d.ContactPhone, d.TargetVehicleModel, d.ConversionType, d.InstallationNotes
            FROM dbo.Orders o
            LEFT JOIN dbo.OrderDetails d ON d.OrderId = o.Id
            WHERE o.CustomerId = @CustomerId AND o.IsDeleted = 0
            ORDER BY o.OrderDate DESC";

        var cmd = new CommandDefinition(sql, new { CustomerId = customerId }, Transaction, cancellationToken: ct);
        var result = await Connection.QueryAsync<Order, OrderDetails?, Order>(
            cmd,
            (order, details) =>
            {
                order.Details = details;
                return order;
            },
            splitOn: "OrderId");

        return result.AsList();
    }

    // Виклик збережуваної процедури підтвердження замовлення
    public async Task ConfirmAsync(long orderId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        try
        {
            var cmd = new CommandDefinition(
                "dbo.usp_ConfirmOrder",
                new { OrderId = orderId, UpdatedBy = "manager" },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex, orderId);
        }
    }

    public async Task PayAsync(long orderId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        try
        {
            var cmd = new CommandDefinition(
                "dbo.usp_PayOrder",
                new { OrderId = orderId, UpdatedBy = "payment-gateway" },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex, orderId);
        }
    }

    public async Task ShipAsync(long orderId, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        try
        {
            var cmd = new CommandDefinition(
                "dbo.usp_ShipOrder",
                new { OrderId = orderId, UpdatedBy = "warehouse" },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex, orderId);
        }
    }

    public async Task CancelAsync(long orderId, string reason = "Скасовано клієнтом", CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        try
        {
            var cmd = new CommandDefinition(
                "dbo.usp_CancelOrder",
                new { OrderId = orderId, CancellationReason = reason, UpdatedBy = "customer" },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex, orderId);
        }
    }

    public async Task AddItemAsync(long orderId, long productId, int quantity, decimal discount = 0, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        try
        {
            var cmd = new CommandDefinition(
                "dbo.usp_AddOrderItem",
                new { OrderId = orderId, ProductId = productId, Quantity = quantity, Discount = discount },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex)
        {
            throw TranslateSqlException(ex, orderId);
        }
    }

    private static Exception TranslateSqlException(SqlException ex, long orderId)
    {
        return ex.Number switch
        {
            50001 => new NotFoundException($"Замовлення {orderId} не знайдено."),
            50002 => new BusinessConflictException(ex.Message),
            50003 => new BusinessConflictException(ex.Message),
            50004 => new BusinessConflictException(ex.Message),
            50005 => new BusinessConflictException(ex.Message),
            50010 => new NotFoundException(ex.Message),
            50020 => new ValidationException(ex.Message),
            50021 => new BusinessConflictException(ex.Message),
            50022 => new NotFoundException(ex.Message),
            _ => ex
        };
    }
}
