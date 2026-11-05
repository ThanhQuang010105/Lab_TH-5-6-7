using QTKD.Application.DTOs;

namespace QTKD.Application.Interfaces;

public interface IPriceListService
{
    Task<List<PriceListDto>> GetAllAsync(CancellationToken ct = default);
    Task<PriceListDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PriceListDto> CreateAsync(CreatePriceListRequest req, CancellationToken ct = default);
    Task<PriceItemDto> AddItemAsync(int priceListId, AddPriceItemRequest req, CancellationToken ct = default);
    Task<List<PriceItemDto>> GetItemsAsync(int priceListId, CancellationToken ct = default);
}

