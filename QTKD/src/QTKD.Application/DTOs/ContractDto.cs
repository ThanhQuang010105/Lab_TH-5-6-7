using QTKD.Domain.Enums;

namespace QTKD.Application.DTOs;

// Trả về
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

public record PriceListDto(
    int Id,
    string Name,
    DateOnly EffectiveDate,
    List<PriceItemDto> Items);

public record PriceItemDto(
    int PriceListId,
    string ServiceCode,
    decimal UnitPrice);

//  Nhận vào (Contract) 
public record CreateContractRequest(
    string ContractNo,
    int CustomerId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? ServiceGroup,
    string? Content);

public record UpdateContractRequest(
    int CustomerId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? ServiceGroup,
    string? Content);

public record AddAttachmentRequest(string FileName);

public record SubmitContractRequest(string? PerformedBy, string? Comment);

// ── Nhận vào (PriceList) 
public record CreatePriceListRequest(string Name, DateOnly EffectiveDate);

public record AddPriceItemRequest(string ServiceCode, decimal UnitPrice);

// ── Lỗi nghiệp vụ trả về client 
public record BusinessErrorResponse(string Code, string Message, IReadOnlyList<string> Details);
