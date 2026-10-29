namespace QTKD.Domain.Entities;

public class ContractAttachment
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string FileName { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
}
