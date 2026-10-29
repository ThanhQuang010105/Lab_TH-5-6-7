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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> GetById(int id, CancellationToken ct)
    {
        var contract = await _service.GetByIdAsync(id, ct);
        return contract is null ? NotFound(new { message = "Không tìm thấy hợp đồng." }) : Ok(contract);
    }
}
