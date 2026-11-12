using EVShop.Ordering.Bll.DTOs;
using EVShop.Ordering.Bll.Interfaces;
using EVShop.Ordering.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EVShop.Ordering.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Отримати список усіх клієнтів (чистий ADO.NET)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct)
    {
        var customers = await _customerService.GetAllAsync(ct);
        return Ok(customers);
    }

    /// <summary>
    /// Отримати дані клієнта за ID (демонструє роботу репозиторію на чистому ADO.NET)
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken ct)
    {
        var customer = await _customerService.GetByIdAsync(id, ct);
        if (customer == null)
            throw new NotFoundException($"Клієнта з ID {id} не знайдено.");
        

        return Ok(customer);
    }

    /// <summary>
    /// Створити нового клієнта (чистий ADO.NET)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto input, CancellationToken ct)
    {
        var created = await _customerService.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
