using Microsoft.AspNetCore.Mvc;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;
    public CustomersController(ICustomerService service) => _service = service;

    // GET /api/customers
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    // GET /api/customers/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct)
    {
        var customer = await _service.GetByIdAsync(id, ct);
        return customer is null
            ? NotFound(new { message = "Không tìm thấy khách hàng." })
            : Ok(customer);
    }

    // POST /api/customers
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest req, CancellationToken ct)
    {
        var created = await _service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/customers/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(
        int id, [FromBody] UpdateCustomerRequest req, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, req, ct);
        return updated is null
            ? NotFound(new { message = "Không tìm thấy khách hàng." })
            : Ok(updated);
    }
}
