using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Bll.Interfaces;
using EVShop.Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EVShop.Catalog.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Отримати список усіх категорій
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
    {
        var categories = await _categoryService.GetAllAsync(ct);
        return Ok(categories);
    }

    /// <summary>
    /// Отримати категорію разом з товарами (Демонстрація Explicit Loading, 2 SQL-запити, Вимога 2.2)
    /// </summary>
    [HttpGet("{id:int}/products")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetWithProducts(int id, CancellationToken ct)
    {
        var category = await _categoryService.GetWithProductsAsync(id, ct);
        if (category is null)
            throw new EntityNotFoundException($"Категорію з ID {id} не знайдено");

        return Ok(category);
    }

    /// <summary>
    /// Отримати нещодавно додані товари категорії (Демонстрація LINQ M:N через поле AddedAt, Вимога 2.2)
    /// </summary>
    [HttpGet("recently-added")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetRecentlyAdded(
        [FromQuery] string category = "Electric Motors",
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var products = await _categoryService.GetRecentlyAddedAsync(category, since, ct);
        return Ok(products);
    }
}
