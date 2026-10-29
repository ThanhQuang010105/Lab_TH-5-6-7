using QTKD.Application.DTOs;

namespace QTKD.Application.Interfaces;

public interface IContractService
{
    Task<ContractDto?> GetByIdAsync(int id, CancellationToken ct = default);
    // Tuần 06 sẽ thêm: CreateAsync, UpdateAsync, SubmitAsync
}
