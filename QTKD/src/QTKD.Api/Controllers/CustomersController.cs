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

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));
}
