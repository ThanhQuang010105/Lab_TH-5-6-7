using Microsoft.EntityFrameworkCore;
using QTKD.Domain.Entities;

namespace QTKD.Application.Interfaces;

public interface IQtkdDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractAttachment> ContractAttachments { get; }
    DbSet<ContractHistory> ContractHistories { get; }
    DbSet<PriceList> PriceLists { get; }
    DbSet<PriceItem> PriceItems { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
