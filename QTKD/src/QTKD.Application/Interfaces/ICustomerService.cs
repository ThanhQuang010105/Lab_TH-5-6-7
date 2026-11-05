using QTKD.Application.DTOs;

namespace QTKD.Application.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest req, CancellationToken ct = default);
    Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequest req, CancellationToken ct = default);
}
