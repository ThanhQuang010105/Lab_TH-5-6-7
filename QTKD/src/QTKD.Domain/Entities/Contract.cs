using QTKD.Domain.Enums;

namespace QTKD.Domain.Entities;

public class Contract
{
    public int Id { get; set; }
    public string ContractNo { get; set; } = default!;
    public int CustomerId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? ServiceGroup { get; set; }          // ví dụ: "Dịch vụ cảng"
    public string? Content { get; set; }               // nội dung bắt buộc (CTR-02)
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public DateTime? SubmittedAt { get; set; }

    public Customer Customer { get; set; } = default!;
    public List<ContractAttachment> Attachments { get; set; } = new();
    public List<ContractHistory> Histories { get; set; } = new();
}
