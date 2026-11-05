using QTKD.Application.DTOs;

namespace QTKD.Application.Interfaces;

public interface IContractService
{
    Task<ContractDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ContractDto> CreateAsync(CreateContractRequest req, CancellationToken ct = default);
    Task<ContractDto?> UpdateAsync(int id, UpdateContractRequest req, CancellationToken ct = default);
    Task<AttachmentDto> AddAttachmentAsync(int contractId, AddAttachmentRequest req, CancellationToken ct = default);
    Task<ContractDto> SubmitAsync(int id, SubmitContractRequest req, CancellationToken ct = default);
}
