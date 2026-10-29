using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;

namespace QTKD.Application.Services;

public class ContractService : IContractService
{
    private readonly IQtkdDbContext _db;
    public ContractService(IQtkdDbContext db) => _db = db;

    public Task<ContractDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Contracts.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ContractDto(
                c.Id, c.ContractNo, c.CustomerId, c.Customer.Name,
                c.StartDate, c.EndDate, c.ServiceGroup, c.Content,
                c.Status, c.SubmittedAt,
                c.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName)).ToList()))
            .FirstOrDefaultAsync(ct);
}
