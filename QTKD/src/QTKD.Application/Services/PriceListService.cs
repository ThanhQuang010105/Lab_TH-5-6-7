using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;
using QTKD.Domain.Entities;
using QTKD.Domain.Exceptions;

namespace QTKD.Application.Services;

public class PriceListService : IPriceListService
{
    private readonly IQtkdDbContext _db;
    public PriceListService(IQtkdDbContext db) => _db = db;

    // ── Helper 
    private static PriceListDto ToDto(PriceList pl) =>
        new(pl.Id, pl.Name, pl.EffectiveDate,
            pl.Items.Select(i => new PriceItemDto(i.PriceListId, i.ServiceCode, i.UnitPrice)).ToList());

    // ── GET ALL 
    public Task<List<PriceListDto>> GetAllAsync(CancellationToken ct = default) =>
        _db.PriceLists.AsNoTracking()
            .Include(pl => pl.Items)
            .OrderByDescending(pl => pl.EffectiveDate)
            .Select(pl => new PriceListDto(
                pl.Id, pl.Name, pl.EffectiveDate,
                pl.Items.Select(i => new PriceItemDto(i.PriceListId, i.ServiceCode, i.UnitPrice)).ToList()))
            .ToListAsync(ct);

    // ── GET BY ID 
    public Task<PriceListDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.PriceLists.AsNoTracking()
            .Include(pl => pl.Items)
            .Where(pl => pl.Id == id)
            .Select(pl => new PriceListDto(
                pl.Id, pl.Name, pl.EffectiveDate,
                pl.Items.Select(i => new PriceItemDto(i.PriceListId, i.ServiceCode, i.UnitPrice)).ToList()))
            .FirstOrDefaultAsync(ct)!;

    // ── CREATE 
    public async Task<PriceListDto> CreateAsync(CreatePriceListRequest req, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("Name không được để trống.");
        if (errors.Any())
            throw new BusinessRuleException("VALIDATION", "Dữ liệu không hợp lệ.", errors);

        var priceList = new PriceList
        {
            Name          = req.Name.Trim(),
            EffectiveDate = req.EffectiveDate
        };

        _db.PriceLists.Add(priceList);
        await _db.SaveChangesAsync(ct);

        return ToDto(priceList);
    }

    //  ADD ITEM 
    public async Task<PriceItemDto> AddItemAsync(int priceListId, AddPriceItemRequest req, CancellationToken ct = default)
    {
        var priceList = await _db.PriceLists
            .Include(pl => pl.Items)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId, ct);

        if (priceList is null)
            throw new BusinessRuleException("NOT_FOUND", "Bảng giá không tồn tại.");

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.ServiceCode)) errors.Add("ServiceCode không được để trống.");
        if (req.UnitPrice < 0)                          errors.Add("UnitPrice phải >= 0.");
        if (errors.Any())
            throw new BusinessRuleException("VALIDATION", "Dữ liệu không hợp lệ.", errors);

        // Kiểm tra trùng ServiceCode trong cùng bảng giá
        if (priceList.Items.Any(i => i.ServiceCode == req.ServiceCode.Trim()))
            throw new BusinessRuleException("DUPLICATE_CODE",
                $"ServiceCode '{req.ServiceCode}' đã tồn tại trong bảng giá này.");

        var item = new PriceItem
        {
            PriceListId  = priceListId,
            ServiceCode  = req.ServiceCode.Trim(),
            UnitPrice    = req.UnitPrice
        };

        _db.PriceItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return new PriceItemDto(item.PriceListId, item.ServiceCode, item.UnitPrice);
    }

    //  GET ITEMS 
    public Task<List<PriceItemDto>> GetItemsAsync(int priceListId, CancellationToken ct = default) =>
        _db.PriceItems.AsNoTracking()
            .Where(i => i.PriceListId == priceListId)
            .Select(i => new PriceItemDto(i.PriceListId, i.ServiceCode, i.UnitPrice))
            .ToListAsync(ct);
}

