using QTKD.Domain.Enums;

namespace QTKD.Application.DTOs;

public record CustomerDto(int Id, string Code, string Name, string? TaxCode, string? Address, CustomerStatus Status);
