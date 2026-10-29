using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QTKD.Domain.Entities;

namespace QTKD.Infrastructure.Persistence.Configurations;

public class ContractAttachmentConfiguration : IEntityTypeConfiguration<ContractAttachment>
{
    public void Configure(EntityTypeBuilder<ContractAttachment> b)
    {
        b.ToTable("contract_attachments");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(260).IsRequired();
    }
}
