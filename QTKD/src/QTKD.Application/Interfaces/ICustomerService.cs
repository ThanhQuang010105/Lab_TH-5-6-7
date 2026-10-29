using QTKD.Application.DTOs;

namespace QTKD.Application.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(CancellationToken ct = default);
}
