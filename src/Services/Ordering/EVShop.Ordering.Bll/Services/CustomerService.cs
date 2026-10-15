using AutoMapper;
using EVShop.Ordering.Bll.DTOs;
using EVShop.Ordering.Bll.Interfaces;
using EVShop.Ordering.Dal.Interfaces;
using EVShop.Ordering.Domain.Entities;

namespace EVShop.Ordering.Bll.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<CustomerDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        return customer == null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            Email = input.Email.Trim().ToLowerInvariant(),
            FullName = input.FullName.Trim(),
            PhoneNumber = input.PhoneNumber?.Trim(),
            CreatedBy = "web-api"
        };

        var id = await _uow.Customers.AddAsync(customer, ct);
        customer.Id = id;

        return _mapper.Map<CustomerDto>(customer);
    }
}
