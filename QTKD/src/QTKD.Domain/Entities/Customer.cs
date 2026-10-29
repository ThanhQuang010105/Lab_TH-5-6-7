using QTKD.Domain.Enums;

namespace QTKD.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public List<Contract> Contracts { get; set; } = new();
}
