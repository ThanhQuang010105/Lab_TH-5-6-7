using Microsoft.AspNetCore.Mvc;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/pricelists")]
public class PriceListsController : ControllerBase
{
    private readonly IPriceListService _service;
    public PriceListsController(IPriceListService service) => _service = service;

    // GET /api/pricelists
    [HttpGet]
    public async Task<ActionResult<List<PriceListDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    // GET /api/pricelists/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PriceListDto>> GetById(int id, CancellationToken ct)
    {
        var priceList = await _service.GetByIdAsync(id, ct);
        return priceList is null
            ? NotFound(new { message = "Không tìm thấy bảng giá." })
            : Ok(priceList);
    }

    // POST /api/pricelists
    [HttpPost]
    public async Task<ActionResult<PriceListDto>> Create(
        [FromBody] CreatePriceListRequest req, CancellationToken ct)
    {
        var created = await _service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // GET /api/pricelists/{id}/items
    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<List<PriceItemDto>>> GetItems(int id, CancellationToken ct) =>
        Ok(await _service.GetItemsAsync(id, ct));

    // POST /api/pricelists/{id}/items
    [HttpPost("{id:int}/items")]
    public async Task<ActionResult<PriceItemDto>> AddItem(
        int id, [FromBody] AddPriceItemRequest req, CancellationToken ct)
    {
        var item = await _service.AddItemAsync(id, req, ct);
        return Ok(item);
    }
}

