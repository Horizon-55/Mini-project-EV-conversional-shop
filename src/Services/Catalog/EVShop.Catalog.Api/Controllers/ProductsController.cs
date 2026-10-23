using EVShop.Catalog.Bll.DTOs;
using EVShop.Catalog.Bll.Interfaces;
using EVShop.Catalog.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EVShop.Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Отримати список товарів з пагінацією, фільтрацією та сортуванням (Вимога 4.2 та 4.6)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetList(
        [FromQuery] ProductQuery query,
        CancellationToken ct)
    {
        var result = await _productService.GetListAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Отримати детальну інформацію про товар за ID (Eager Loading, Вимога 2.2)
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        var product = await _productService.GetByIdAsync(id, ct);
        if (product is null)
            throw new EntityNotFoundException($"Товар з ID {id} не знайдено");

        return Ok(product);
    }

    /// <summary>
    /// Створити новий товар з валідацією через FluentValidation (Вимога 4.3)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto input,
        [FromServices] IValidator<CreateProductDto> validator,
        CancellationToken ct)
    {
        // 1. Явна валідація через FluentValidation (Вимога 4.3 методички)
        var validationResult = await validator.ValidateAsync(input, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        // 2. Бізнес-логіка створення (унікальність SKU перевіряється в сервісі)
        var created = await _productService.CreateAsync(input, ct);

        // 3. Повертаємо 201 Created із заголовком Location (Вимога Частина 3)
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Оновити базову ціну товару
    /// </summary>
    [HttpPatch("{id:int}/price")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdatePrice(
        int id,
        [FromBody] UpdateProductPriceDto input,
        CancellationToken ct)
    {
        var updated = await _productService.UpdatePriceAsync(id, input.NewPrice, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Поповнити складський залишок товару (метод агрегату AddStock)
    /// </summary>
    [HttpPost("{id:int}/stock/add")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> AddStock(
        int id,
        [FromBody] AdjustStockDto input,
        CancellationToken ct)
    {
        var updated = await _productService.AddStockAsync(id, input.Quantity, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Списати кількість товару зі складу (метод агрегату RemoveStock, кидає 409 при нестачі)
    /// </summary>
    [HttpPost("{id:int}/stock/remove")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> RemoveStock(
        int id,
        [FromBody] AdjustStockDto input,
        CancellationToken ct)
    {
        var updated = await _productService.RemoveStockAsync(id, input.Quantity, ct);
        return Ok(updated);
    }
}
