using EVShop.Catalog.Dal.Context;
using EVShop.Catalog.Dal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVShop.Catalog.Dal.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly CatalogDbContext Context;
    protected readonly DbSet<T> Set;

    public GenericRepository(CatalogDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await Set.FindAsync(new object[] { id }, ct);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await Set.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await Set.AddAsync(entity, ct);
    }

    public virtual void Update(T entity)
    {
        Set.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        Set.Remove(entity);
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await Context.SaveChangesAsync(ct);
    }
}
