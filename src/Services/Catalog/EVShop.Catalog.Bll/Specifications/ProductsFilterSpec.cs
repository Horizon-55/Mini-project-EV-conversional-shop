using Ardalis.Specification;
using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Domain.Entities;

namespace EVShop.Catalog.Bll.Specifications;

// Специфікація для посторінкової вибірки товарів з фільтрацією та сортуванням (Вимога 4.6 методички)
public class ProductsFilterSpec : Specification<Product>
{
    public ProductsFilterSpec(ProductQuery q, int pageSize)
    {
        Query.Include(p => p.Brand)
             .Include(p => p.Detail)
             .Include(p => p.Images)
             .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            Query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        if (q.MinPrice is not null)
            Query.Where(p => p.Price >= q.MinPrice.Value);

        if (q.MaxPrice is not null)
            Query.Where(p => p.Price <= q.MaxPrice.Value);

        if (q.BrandId is not null)
            Query.Where(p => p.BrandId == q.BrandId.Value);

        if (q.CategoryId is not null)
            Query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == q.CategoryId.Value));

        if (string.Equals(q.SortBy, "price", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase))
                Query.OrderByDescending(p => p.Price);
            else
                Query.OrderBy(p => p.Price);
        }
        else
        {
            if (string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase))
                Query.OrderByDescending(p => p.Name);
            else
                Query.OrderBy(p => p.Name);
        }

        var skip = Math.Max(0, (q.Page - 1) * pageSize);
        Query.Skip(skip).Take(pageSize);
    }
}

// Специфікація для підрахунку загальної кількості записів за тими ж критеріями (без пагінації та include)
public class ProductsCountSpec : Specification<Product>
{
    public ProductsCountSpec(ProductQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            Query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        if (q.MinPrice is not null)
            Query.Where(p => p.Price >= q.MinPrice.Value);

        if (q.MaxPrice is not null)
            Query.Where(p => p.Price <= q.MaxPrice.Value);

        if (q.BrandId is not null)
            Query.Where(p => p.BrandId == q.BrandId.Value);

        if (q.CategoryId is not null)
            Query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == q.CategoryId.Value));
    }
}
