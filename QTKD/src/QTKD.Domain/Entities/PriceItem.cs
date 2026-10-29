namespace QTKD.Domain.Entities;

public class PriceItem
{
    public int PriceListId { get; set; }
    public string ServiceCode { get; set; } = default!;
    public decimal UnitPrice { get; set; }
}
