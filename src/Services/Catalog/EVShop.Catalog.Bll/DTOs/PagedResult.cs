namespace EVShop.Catalog.Bll.DTOs;

// Модель запиту для фільтрації, сортування та пагінації (Вимога 4.2 методички)
public record ProductQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string SortBy = "name",
    string SortDir = "asc",
    int? CategoryId = null,
    int? BrandId = null
);

// Контракт посторінкової відповіді з метаданими (Вимога 4.2 методички)
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
