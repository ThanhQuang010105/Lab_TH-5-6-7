using Microsoft.AspNetCore.Mvc;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractService _service;
    public ContractsController(IContractService service) => _service = service;

    // GET /api/contracts/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> GetById(int id, CancellationToken ct)
    {
        var contract = await _service.GetByIdAsync(id, ct);
        return contract is null
            ? NotFound(new { message = "Không tìm thấy hợp đồng." })
            : Ok(contract);
    }

    // POST /api/contracts
    [HttpPost]
    public async Task<ActionResult<ContractDto>> Create(
        [FromBody] CreateContractRequest req, CancellationToken ct)
    {
        var created = await _service.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/contracts/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContractDto>> Update(
        int id, [FromBody] UpdateContractRequest req, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, req, ct);
        return updated is null
            ? NotFound(new { message = "Không tìm thấy hợp đồng." })
            : Ok(updated);
    }

    // POST /api/contracts/{id}/attachments
    [HttpPost("{id:int}/attachments")]
    public async Task<ActionResult<AttachmentDto>> AddAttachment(
        int id, [FromBody] AddAttachmentRequest req, CancellationToken ct)
    {
        var attachment = await _service.AddAttachmentAsync(id, req, ct);
        return Ok(attachment);
    }

    // POST /api/contracts/{id}/submit  (US03)
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<ContractDto>> Submit(
        int id, [FromBody] SubmitContractRequest req, CancellationToken ct)
    {
        var updated = await _service.SubmitAsync(id, req, ct);
        return Ok(updated);
    }
}
