using QTKD.Domain.Enums;

namespace QTKD.Domain.Entities;

public class ContractHistory
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string Action { get; set; } = default!;     // "Created", "Submitted", ...
    public ContractStatus? FromStatus { get; set; }
    public ContractStatus? ToStatus { get; set; }
    public string PerformedBy { get; set; } = default!;
    public DateTime PerformedAt { get; set; }
    public string? Note { get; set; }
}
