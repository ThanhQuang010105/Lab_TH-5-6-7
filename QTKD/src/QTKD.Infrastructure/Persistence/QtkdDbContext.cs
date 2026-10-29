using Microsoft.EntityFrameworkCore;
using QTKD.Application.Interfaces;
using QTKD.Domain.Entities;

namespace QTKD.Infrastructure.Persistence;

public class QtkdDbContext : DbContext, IQtkdDbContext
{
    public QtkdDbContext(DbContextOptions<QtkdDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractAttachment> ContractAttachments => Set<ContractAttachment>();
    public DbSet<ContractHistory> ContractHistories => Set<ContractHistory>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceItem> PriceItems => Set<PriceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QtkdDbContext).Assembly);
}
