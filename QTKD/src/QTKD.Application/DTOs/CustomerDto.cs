using QTKD.Domain.Enums;

namespace QTKD.Application.DTOs;

// ── Trả về 
public record CustomerDto(
    int Id,
    string Code,
    string Name,
    string? TaxCode,
    string? Address,
    CustomerStatus Status);

// ── Nhận vào (tạo / cập nhật) 
public record CreateCustomerRequest(
    string Code,
    string Name,
    string? TaxCode,
    string? Address);

public record UpdateCustomerRequest(
    string Name,
    string? TaxCode,
    string? Address,
    CustomerStatus Status);
