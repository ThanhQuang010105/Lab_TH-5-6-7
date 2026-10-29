using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;

namespace QTKD.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IQtkdDbContext _db;
    public CustomerService(IQtkdDbContext db) => _db = db;

    public Task<List<CustomerDto>> GetAllAsync(CancellationToken ct = default) =>
        _db.Customers.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CustomerDto(c.Id, c.Code, c.Name, c.TaxCode, c.Address, c.Status))
            .ToListAsync(ct);
}
