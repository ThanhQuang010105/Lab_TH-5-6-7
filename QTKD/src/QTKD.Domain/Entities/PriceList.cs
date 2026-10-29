namespace QTKD.Domain.Entities;

public class PriceList
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public DateOnly EffectiveDate { get; set; }
    public List<PriceItem> Items { get; set; } = new();
}
