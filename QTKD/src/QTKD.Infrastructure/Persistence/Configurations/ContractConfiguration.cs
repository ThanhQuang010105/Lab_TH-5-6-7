using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QTKD.Domain.Entities;

namespace QTKD.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> b)
    {
        b.ToTable("contracts");
        b.HasKey(x => x.Id);
        b.Property(x => x.ContractNo).HasMaxLength(50).IsRequired();
        b.Property(x => x.ServiceGroup).HasMaxLength(100);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => x.ContractNo).IsUnique();

        b.HasOne(x => x.Customer)
         .WithMany(c => c.Contracts)
         .HasForeignKey(x => x.CustomerId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Attachments).WithOne()
         .HasForeignKey(a => a.ContractId).OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Histories).WithOne()
         .HasForeignKey(h => h.ContractId).OnDelete(DeleteBehavior.Cascade);
    }
}
