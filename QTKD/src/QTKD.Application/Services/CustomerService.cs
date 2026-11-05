using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;
using QTKD.Domain.Entities;
using QTKD.Domain.Enums;
using QTKD.Domain.Exceptions;

namespace QTKD.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IQtkdDbContext _db;
    public CustomerService(IQtkdDbContext db) => _db = db;

    // ── GET ALL ──────────────────────────────────────────────────────────────
    public Task<List<CustomerDto>> GetAllAsync(CancellationToken ct = default) =>
        _db.Customers.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CustomerDto(c.Id, c.Code, c.Name, c.TaxCode, c.Address, c.Status))
            .ToListAsync(ct);

    // ── GET BY ID ─────────────────────────────────────────────────────────────
    public Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Customers.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDto(c.Id, c.Code, c.Name, c.TaxCode, c.Address, c.Status))
            .FirstOrDefaultAsync(ct)!;

    // ── CREATE ────────────────────────────────────────────────────────────────
    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest req, CancellationToken ct = default)
    {
        // Validate bắt buộc
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.Code))  errors.Add("Code không được để trống.");
        if (string.IsNullOrWhiteSpace(req.Name))  errors.Add("Name không được để trống.");
        if (errors.Any())
            throw new BusinessRuleException("VALIDATION", "Dữ liệu không hợp lệ.", errors);

        // Kiểm tra trùng Code
        var exists = await _db.Customers.AnyAsync(c => c.Code == req.Code.Trim(), ct);
        if (exists)
            throw new BusinessRuleException("DUPLICATE_CODE", $"Mã khách hàng '{req.Code}' đã tồn tại.");

        var customer = new Customer
        {
            Code    = req.Code.Trim(),
            Name    = req.Name.Trim(),
            TaxCode = req.TaxCode?.Trim(),
            Address = req.Address?.Trim(),
            Status  = CustomerStatus.Active
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return new CustomerDto(customer.Id, customer.Code, customer.Name,
                               customer.TaxCode, customer.Address, customer.Status);
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────
    public async Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequest req, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { id }, ct);
        if (customer is null) return null;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("Name không được để trống.");
        if (errors.Any())
            throw new BusinessRuleException("VALIDATION", "Dữ liệu không hợp lệ.", errors);

        customer.Name    = req.Name.Trim();
        customer.TaxCode = req.TaxCode?.Trim();
        customer.Address = req.Address?.Trim();
        customer.Status  = req.Status;

        await _db.SaveChangesAsync(ct);

        return new CustomerDto(customer.Id, customer.Code, customer.Name,
                               customer.TaxCode, customer.Address, customer.Status);
    }
}
