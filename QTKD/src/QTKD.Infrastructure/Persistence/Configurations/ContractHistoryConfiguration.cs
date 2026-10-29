using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QTKD.Domain.Entities;

namespace QTKD.Infrastructure.Persistence.Configurations;

public class ContractHistoryConfiguration : IEntityTypeConfiguration<ContractHistory>
{
    public void Configure(EntityTypeBuilder<ContractHistory> b)
    {
        b.ToTable("contract_histories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(50).IsRequired();
        b.Property(x => x.PerformedBy).HasMaxLength(100).IsRequired();
        b.Property(x => x.Note).HasMaxLength(1000);
        b.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30);
    }
}
