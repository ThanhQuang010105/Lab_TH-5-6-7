using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Interfaces;
using QTKD.Domain.Entities;
using QTKD.Domain.Enums;
using QTKD.Domain.Exceptions;

namespace QTKD.Application.Services;

public class ContractService : IContractService
{
    private readonly IQtkdDbContext _db;
    public ContractService(IQtkdDbContext db) => _db = db;

    // Helper: chiếu Contract -> ContractDto 
    private static ContractDto ToDto(Contract c) =>
        new(c.Id, c.ContractNo, c.CustomerId, c.Customer.Name,
            c.StartDate, c.EndDate, c.ServiceGroup, c.Content,
            c.Status, c.SubmittedAt,
            c.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName)).ToList());

    //  GET BY ID
    public Task<ContractDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Contracts.AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Attachments)
            .Where(c => c.Id == id)
            .Select(c => new ContractDto(
                c.Id, c.ContractNo, c.CustomerId, c.Customer.Name,
                c.StartDate, c.EndDate, c.ServiceGroup, c.Content,
                c.Status, c.SubmittedAt,
                c.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName)).ToList()))
            .FirstOrDefaultAsync(ct)!;

    //  CREATE 
    public async Task<ContractDto> CreateAsync(CreateContractRequest req, CancellationToken ct = default)
    {
        // Validate cơ bản
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.ContractNo)) errors.Add("ContractNo không được để trống.");
        if (req.StartDate > req.EndDate)                errors.Add("StartDate phải <= EndDate.");
        if (errors.Any())
            throw new BusinessRuleException("VALIDATION", "Dữ liệu không hợp lệ.", errors);

        // Kiểm tra ContractNo trùng
        if (await _db.Contracts.AnyAsync(c => c.ContractNo == req.ContractNo.Trim(), ct))
            throw new BusinessRuleException("DUPLICATE_NO", $"Số hợp đồng '{req.ContractNo}' đã tồn tại.");

        // Kiểm tra Customer tồn tại & Active
        var customer = await _db.Customers.FindAsync(new object[] { req.CustomerId }, ct);
        if (customer is null)
            throw new BusinessRuleException("CUSTOMER_NOT_FOUND", "Khách hàng không tồn tại.");
        if (customer.Status != CustomerStatus.Active)
            throw new BusinessRuleException("CUSTOMER_INACTIVE", "Khách hàng không ở trạng thái Active.");

        var contract = new Contract
        {
            ContractNo   = req.ContractNo.Trim(),
            CustomerId   = req.CustomerId,
            StartDate    = req.StartDate,
            EndDate      = req.EndDate,
            ServiceGroup = req.ServiceGroup?.Trim(),
            Content      = req.Content?.Trim(),
            Status       = ContractStatus.Draft
        };

        contract.Histories.Add(new ContractHistory
        {
            Action      = "Created",
            ToStatus    = ContractStatus.Draft,
            PerformedBy = "api",
            PerformedAt = DateTime.UtcNow
        });

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync(ct);

        contract.Customer = customer;
        return ToDto(contract);
    }

    // UPDATE (kiểm tra CTR-01) 
    public async Task<ContractDto?> UpdateAsync(int id, UpdateContractRequest req, CancellationToken ct = default)
    {
        var contract = await _db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (contract is null) return null;

        //  CTR-01: chỉ sửa khi Draft hoặc RevisionRequested 
        if (contract.Status is not ContractStatus.Draft and not ContractStatus.RevisionRequested)
            throw new BusinessRuleException(
                "CTR-01",
                "Vi phạm CTR-01: chỉ có thể cập nhật hợp đồng ở trạng thái Draft hoặc Revision Requested.",
                new[] { $"Trạng thái hiện tại: {contract.Status}" });

        // Validate
        var errors = new List<string>();
        if (req.StartDate > req.EndDate) errors.Add("StartDate phải <= EndDate.");
        if (errors.Any())
            throw new BusinessRuleException("VALIDATION", "Dữ liệu không hợp lệ.", errors);

        // Kiểm tra Customer nếu thay đổi
        if (req.CustomerId != contract.CustomerId)
        {
            var customer = await _db.Customers.FindAsync(new object[] { req.CustomerId }, ct);
            if (customer is null)
                throw new BusinessRuleException("CUSTOMER_NOT_FOUND", "Khách hàng không tồn tại.");
            if (customer.Status != CustomerStatus.Active)
                throw new BusinessRuleException("CUSTOMER_INACTIVE", "Khách hàng không ở trạng thái Active.");

            contract.CustomerId = req.CustomerId;
            contract.Customer   = customer;
        }

        contract.StartDate    = req.StartDate;
        contract.EndDate      = req.EndDate;
        contract.ServiceGroup = req.ServiceGroup?.Trim();
        contract.Content      = req.Content?.Trim();

        await _db.SaveChangesAsync(ct);
        return ToDto(contract);
    }

    //  ADD ATTACHMENT 
    public async Task<AttachmentDto> AddAttachmentAsync(int contractId, AddAttachmentRequest req, CancellationToken ct = default)
    {
        var contract = await _db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == contractId, ct);

        if (contract is null)
            throw new BusinessRuleException("NOT_FOUND", "Hợp đồng không tồn tại.");

        // CTR-01: không thêm tệp nếu hợp đồng đã qua giai đoạn Draft/RevisionRequested
        if (contract.Status is not ContractStatus.Draft and not ContractStatus.RevisionRequested)
            throw new BusinessRuleException(
                "CTR-01",
                "Vi phạm CTR-01: chỉ có thể thêm tệp khi hợp đồng ở trạng thái Draft hoặc Revision Requested.",
                new[] { $"Trạng thái hiện tại: {contract.Status}" });

        if (string.IsNullOrWhiteSpace(req.FileName))
            throw new BusinessRuleException("VALIDATION", "FileName không được để trống.");

        var attachment = new ContractAttachment
        {
            ContractId  = contractId,
            FileName    = req.FileName.Trim(),
            UploadedAt  = DateTime.UtcNow
        };

        _db.ContractAttachments.Add(attachment);
        await _db.SaveChangesAsync(ct);

        return new AttachmentDto(attachment.Id, attachment.FileName);
    }

    // SUBMIT (US03 / CTR-02 / CTR-03)
    public async Task<ContractDto> SubmitAsync(int id, SubmitContractRequest req, CancellationToken ct = default)
    {
        var contract = await _db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (contract is null)
            throw new BusinessRuleException("NOT_FOUND", "Hợp đồng không tồn tại.");

        //  Kiểm tra trạng thái cho phép gửi (AC02) 
        if (contract.Status is not ContractStatus.Draft and not ContractStatus.RevisionRequested)
            throw new BusinessRuleException(
                "WRONG_STATE",
                "Hợp đồng không ở trạng thái cho phép gửi trình duyệt.",
                new[] { $"Trạng thái hiện tại: {contract.Status}" });

        //  CTR-02: kiểm tra điều kiện gửi 
        var violations = new List<string>();

        // 1) Khách hàng hợp lệ
        if (contract.Customer is null || contract.Customer.Status != CustomerStatus.Active)
            violations.Add("Khách hàng không hợp lệ hoặc đang bị tạm ngưng (Suspended).");

        // 2) Thời gian hiệu lực hợp lệ
        if (contract.StartDate > contract.EndDate)
            violations.Add("Ngày hiệu lực không hợp lệ: StartDate phải <= EndDate.");

        // 3) Có nội dung HOẶC tệp đính kèm
        bool hasContent     = !string.IsNullOrWhiteSpace(contract.Content);
        bool hasAttachment  = contract.Attachments.Any();
        if (!hasContent && !hasAttachment)
            violations.Add("Phải có nội dung hợp đồng (Content) hoặc ít nhất một tệp đính kèm.");

        if (violations.Any())
            throw new BusinessRuleException("CTR-02",
                "Hợp đồng chưa đáp ứng điều kiện gửi trình duyệt (CTR-02).", violations);

        // CTR-03: không chuyển Draft → Approved trực tiếp 
        // (đảm bảo bằng code: chỉ chuyển sang PendingApproval)

        var fromStatus = contract.Status;
        contract.Status      = ContractStatus.PendingApproval;
        contract.SubmittedAt = DateTime.UtcNow;

        contract.Histories.Add(new ContractHistory
        {
            Action      = "Submitted",
            FromStatus  = fromStatus,
            ToStatus    = ContractStatus.PendingApproval,
            PerformedBy = req.PerformedBy?.Trim() ?? "api",
            PerformedAt = DateTime.UtcNow,
            Note        = req.Comment?.Trim()
        });

        await _db.SaveChangesAsync(ct);
        return ToDto(contract);
    }
}
