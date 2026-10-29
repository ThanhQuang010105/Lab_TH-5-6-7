using QTKD.Domain.Enums;

namespace QTKD.Application.DTOs;

public record AttachmentDto(int Id, string FileName);

public record ContractDto(
    int Id,
    string ContractNo,
    int CustomerId,
    string CustomerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string? ServiceGroup,
    string? Content,
    ContractStatus Status,
    DateTime? SubmittedAt,
    List<AttachmentDto> Attachments);
