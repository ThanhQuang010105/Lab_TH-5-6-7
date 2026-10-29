using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QTKD.Domain.Entities;

namespace QTKD.Infrastructure.Persistence.Configurations;

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> b)
    {
        b.ToTable("price_lists");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasMany(x => x.Items).WithOne()
         .HasForeignKey(i => i.PriceListId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PriceItemConfiguration : IEntityTypeConfiguration<PriceItem>
{
    public void Configure(EntityTypeBuilder<PriceItem> b)
    {
        b.ToTable("price_items");
        b.HasKey(x => new { x.PriceListId, x.ServiceCode });   // khóa kép
        b.Property(x => x.ServiceCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
    }
}
