using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Services;
using static QTKD.UnitTests.TestHelpers;

namespace QTKD.UnitTests;

public class PriceListRulesTests
{
    [Fact]
    public async Task AddItem_Valid_LinkedToCorrectPriceList()
    {
        using var db = NewDb();
        var svc = new PriceListService(db);
        var pl = await svc.CreateAsync(new CreatePriceListRequest("BG 2026", new DateOnly(2026, 1, 1)));

        var item = await svc.AddItemAsync(pl.Id, new AddPriceItemRequest("CONT", 100m));

        Assert.Equal(pl.Id, item.PriceListId);
        Assert.Single(await svc.GetItemsAsync(pl.Id));
    }

    [Fact]
    public async Task AddItem_NegativePrice_Rejected()
    {
        using var db = NewDb();
        var svc = new PriceListService(db);
        var pl = await svc.CreateAsync(new CreatePriceListRequest("BG", new DateOnly(2026, 1, 1)));

        var ex = await AssertRuleAsync(() => svc.AddItemAsync(pl.Id, new AddPriceItemRequest("X", -1m)));

        Assert.Equal("VALIDATION", ex.Code);
        Assert.Equal(0, await db.PriceItems.CountAsync());
    }

    [Fact]
    public async Task AddItem_DuplicateServiceCode_Rejected()
    {
        using var db = NewDb();
        var svc = new PriceListService(db);
        var pl = await svc.CreateAsync(new CreatePriceListRequest("BG", new DateOnly(2026, 1, 1)));
        await svc.AddItemAsync(pl.Id, new AddPriceItemRequest("X", 1m));

        var ex = await AssertRuleAsync(() => svc.AddItemAsync(pl.Id, new AddPriceItemRequest("X", 2m)));

        Assert.Equal("DUPLICATE_CODE", ex.Code);
    }

    [Fact]
    public async Task AddItem_PriceListNotFound_Rejected()
    {
        using var db = NewDb();
        var svc = new PriceListService(db);

        var ex = await AssertRuleAsync(() => svc.AddItemAsync(999, new AddPriceItemRequest("X", 1m)));

        Assert.Equal("NOT_FOUND", ex.Code);
    }
}
