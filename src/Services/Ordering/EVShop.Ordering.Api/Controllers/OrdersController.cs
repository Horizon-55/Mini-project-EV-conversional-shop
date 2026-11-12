using EVShop.Ordering.Bll.DTOs;
using EVShop.Ordering.Bll.Interfaces;
using EVShop.Ordering.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EVShop.Ordering.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Отримати деталі замовлення за ID
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
    {
        var order = await _orderService.GetByIdAsync(id, ct);
        if (order == null)
            throw new NotFoundException($"Замовлення з ID {id} не знайдено.");
        
        return Ok(order);
    }

    /// <summary>
    /// Отримати всі замовлення клієнта
    /// </summary>
    [HttpGet("customer/{customerId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetByCustomer(long customerId, CancellationToken ct)
    {
        var orders = await _orderService.GetByCustomerIdAsync(customerId, ct);
        return Ok(orders);
    }

    /// <summary>
    /// Створити нове замовлення комплектуючих EV
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto input, CancellationToken ct)
    {
        var created = await _orderService.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Підтвердити наявність та зарезервувати деталі на складі (Submitted -> StockConfirmed)
    /// </summary>
    [HttpPost("{id:long}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await _orderService.ConfirmOrderAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Оплатити підтверджене замовлення (StockConfirmed -> Paid)
    /// </summary>
    [HttpPost("{id:long}/pay")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(long id, CancellationToken ct)
    {
        await _orderService.PayOrderAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Відвантажити оплачене замовлення клієнту (Paid -> Shipped)
    /// </summary>
    [HttpPost("{id:long}/ship")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ship(long id, CancellationToken ct)
    {
        await _orderService.ShipOrderAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Скасувати замовлення (дозволено лише до моменту оплати)
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(long id, [FromQuery] string reason = "Скасовано клієнтом", CancellationToken ct = default)
    {
        await _orderService.CancelOrderAsync(id, reason, ct);
        return NoContent();
    }

    /// <summary>
    /// Додати товар до існуючого замовлення у статусі Submitted
    /// </summary>
    [HttpPost("{id:long}/items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItem(long id, [FromBody] CreateOrderItemDto itemInput, CancellationToken ct)
    {
        await _orderService.AddItemAsync(id, itemInput, ct);
        return NoContent();
    }
}
