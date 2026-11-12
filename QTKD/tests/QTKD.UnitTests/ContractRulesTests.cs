using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Services;
using QTKD.Domain.Enums;
using static QTKD.UnitTests.TestHelpers;

namespace QTKD.UnitTests;

/// <summary>Unit test cho CTR-01, CTR-02, CTR-03 (mỗi rule có ca đúng và ca sai).</summary>
public class ContractRulesTests
{
    private static UpdateContractRequest UpdateReq(int customerId, string? content = "Nội dung mới") =>
        new(customerId, new DateOnly(2026, 2, 1), new DateOnly(2027, 1, 31), "Kho bãi", content);

    // ───────────── CTR-01: chỉ sửa khi Draft hoặc RevisionRequested ─────────────

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.RevisionRequested)]
    public async Task CTR01_Update_AllowedStatuses_Succeeds(ContractStatus status)
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust, status);
        var svc = new ContractService(db);

        var result = await svc.UpdateAsync(contract.Id, UpdateReq(cust.Id));

        Assert.NotNull(result);
        Assert.Equal("Nội dung mới", result!.Content);
        Assert.Equal(new DateOnly(2026, 2, 1), result.StartDate);
        Assert.Equal(status, result.Status);                 // trạng thái không đổi
    }

    [Theory]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Cancelled)]
    public async Task CTR01_Update_OtherStatuses_RejectedAndDataUnchanged(ContractStatus status)
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust, status, content: "Gốc");
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.UpdateAsync(contract.Id, UpdateReq(cust.Id)));

        Assert.Equal("CTR-01", ex.Code);
        var inDb = await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == contract.Id);
        Assert.Equal("Gốc", inDb.Content);                   // dữ liệu giữ nguyên
        Assert.Equal(new DateOnly(2026, 1, 1), inDb.StartDate);
        Assert.Equal(status, inDb.Status);
    }

    [Fact]
    public async Task CTR01_AddAttachment_Draft_Succeeds()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust);
        var svc = new ContractService(db);

        var att = await svc.AddAttachmentAsync(contract.Id, new AddAttachmentRequest("x.pdf"));

        Assert.Equal("x.pdf", att.FileName);
        Assert.Equal(1, await db.ContractAttachments.CountAsync(a => a.ContractId == contract.Id));
    }

    [Fact]
    public async Task CTR01_AddAttachment_PendingApproval_Rejected()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust, ContractStatus.PendingApproval);
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.AddAttachmentAsync(contract.Id, new AddAttachmentRequest("x.pdf")));

        Assert.Equal("CTR-01", ex.Code);
        Assert.Equal(0, await db.ContractAttachments.CountAsync());
    }

    // ───────────── CTR-02: điều kiện gửi trình duyệt ─────────────

    [Fact]
    public async Task CTR02_Submit_WithContentOnly_Succeeds()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust, content: "Có nội dung", withAttachment: false);
        var svc = new ContractService(db);

        var result = await svc.SubmitAsync(contract.Id, new SubmitContractRequest("sales01", "ok"));

        Assert.Equal(ContractStatus.PendingApproval, result.Status);
        Assert.NotNull(result.SubmittedAt);
    }

    [Fact]
    public async Task CTR02_Submit_WithAttachmentOnly_Succeeds()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust, content: null, withAttachment: true);
        var svc = new ContractService(db);

        var result = await svc.SubmitAsync(contract.Id, new SubmitContractRequest(null, null));

        Assert.Equal(ContractStatus.PendingApproval, result.Status);
    }

    [Fact]
    public async Task CTR02_Submit_NoContentNoAttachment_Rejected_StaysDraft()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust, content: "   ", withAttachment: false);
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.SubmitAsync(contract.Id, new SubmitContractRequest(null, null)));

        Assert.Equal("CTR-02", ex.Code);
        await AssertUnchangedDraftAsync(db, contract.Id);
    }

    [Fact]
    public async Task CTR02_Submit_SuspendedCustomer_Rejected_StaysDraft()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db, CustomerStatus.Suspended);
        var contract = await AddContractAsync(db, cust);
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.SubmitAsync(contract.Id, new SubmitContractRequest(null, null)));

        Assert.Equal("CTR-02", ex.Code);
        await AssertUnchangedDraftAsync(db, contract.Id);
    }

    [Fact]
    public async Task CTR02_Submit_InvalidDates_Rejected_StaysDraft()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust,
            start: new DateOnly(2027, 1, 1), end: new DateOnly(2026, 1, 1));
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.SubmitAsync(contract.Id, new SubmitContractRequest(null, null)));

        Assert.Equal("CTR-02", ex.Code);
        await AssertUnchangedDraftAsync(db, contract.Id);
    }

    [Fact]
    public async Task CTR02_Submit_MultipleViolations_ListsAllOfThem()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db, CustomerStatus.Suspended);
        var contract = await AddContractAsync(db, cust, content: null,
            start: new DateOnly(2027, 1, 1), end: new DateOnly(2026, 1, 1));
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.SubmitAsync(contract.Id, new SubmitContractRequest(null, null)));

        Assert.Equal(3, ex.Details.Count);
    }

    [Fact]
    public async Task CTR02_Submit_FixingMissingDataThenResubmit_Succeeds()
    {
        // Luồng demo tuần 06: gửi thiếu -> thất bại -> bổ sung -> gửi lại -> thành công
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust, content: null);
        var svc = new ContractService(db);

        await AssertRuleAsync(() => svc.SubmitAsync(contract.Id, new SubmitContractRequest(null, null)));
        await svc.AddAttachmentAsync(contract.Id, new AddAttachmentRequest("hop-dong.pdf"));
        var result = await svc.SubmitAsync(contract.Id, new SubmitContractRequest("sales01", null));

        Assert.Equal(ContractStatus.PendingApproval, result.Status);
    }

    [Fact]
    public async Task Submit_ContractNotFound_ThrowsNotFound()
    {
        using var db = NewDb();
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.SubmitAsync(999, new SubmitContractRequest(null, null)));

        Assert.Equal("NOT_FOUND", ex.Code);
    }

    // ───────────── CTR-03: không chuyển thẳng Draft -> Approved ─────────────

    [Fact]
    public async Task CTR03_Submit_Success_GoesToPendingApproval_NotApproved()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust);
        var svc = new ContractService(db);

        var result = await svc.SubmitAsync(contract.Id, new SubmitContractRequest(null, null));

        Assert.Equal(ContractStatus.PendingApproval, result.Status);
        Assert.NotEqual(ContractStatus.Approved, result.Status);
    }

    [Fact]
    public void CTR03_Transition_DraftToApproved_Rejected()
    {
        var ex = Assert.Throws<QTKD.Domain.Exceptions.BusinessRuleException>(() =>
            QTKD.Domain.Rules.ContractTransitions.EnsureCanTransition(ContractStatus.Draft, ContractStatus.Approved));

        Assert.Equal("CTR-03", ex.Code);
    }

    [Theory]
    [InlineData(ContractStatus.Draft, ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.RevisionRequested, ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.PendingApproval, ContractStatus.Approved)]
    public void CTR03_Transition_ValidPairs_Allowed(ContractStatus from, ContractStatus to)
    {
        QTKD.Domain.Rules.ContractTransitions.EnsureCanTransition(from, to);   // không ném
        Assert.True(QTKD.Domain.Rules.ContractTransitions.CanTransition(from, to));
    }

    // ───────────── TC08: gửi lặp ─────────────

    [Fact]
    public async Task TC08_Submit_Twice_SecondRejected_NoDuplicateHistory()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust);
        var svc = new ContractService(db);

        var first = await svc.SubmitAsync(contract.Id, new SubmitContractRequest("sales01", null));
        var ex = await AssertRuleAsync(() => svc.SubmitAsync(contract.Id, new SubmitContractRequest("sales01", null)));

        Assert.Equal("WRONG_STATE", ex.Code);
        var inDb = await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == contract.Id);
        Assert.Equal(ContractStatus.PendingApproval, inDb.Status);
        Assert.Equal(first.SubmittedAt, inDb.SubmittedAt);                       // thời điểm gửi không đổi
        Assert.Equal(1, await db.ContractHistories.CountAsync(h => h.ContractId == contract.Id && h.Action == "Submitted"));
    }

    // ───────────── Validate khi tạo / sửa hợp đồng ─────────────

    [Fact]
    public async Task Create_SuspendedCustomer_Rejected()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db, CustomerStatus.Suspended);
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.CreateAsync(new CreateContractRequest(
            "HD-X", cust.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null, null)));

        Assert.Equal("CUSTOMER_INACTIVE", ex.Code);
        Assert.Equal(0, await db.Contracts.CountAsync());
    }

    [Fact]
    public async Task Create_DuplicateContractNo_Rejected()
    {
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var svc = new ContractService(db);
        var req = new CreateContractRequest("HD-DUP", cust.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null, null);

        await svc.CreateAsync(req);
        var ex = await AssertRuleAsync(() => svc.CreateAsync(req));

        Assert.Equal("DUPLICATE_NO", ex.Code);
        Assert.Equal(1, await db.Contracts.CountAsync());
    }

    [Fact]
    public async Task Update_ContractOfSuspendedCustomer_Rejected()
    {
        // Phát hiện ở tuần 07: trước đây chỉ kiểm tra Customer khi đổi sang khách khác
        using var db = NewDb();
        var cust = await AddCustomerAsync(db);
        var contract = await AddContractAsync(db, cust);
        cust.Status = CustomerStatus.Suspended;
        await db.SaveChangesAsync();
        var svc = new ContractService(db);

        var ex = await AssertRuleAsync(() => svc.UpdateAsync(contract.Id, UpdateReq(cust.Id)));

        Assert.Equal("CUSTOMER_INACTIVE", ex.Code);
    }

    // ───────────── helper ─────────────

    private static async Task AssertUnchangedDraftAsync(QTKD.Infrastructure.Persistence.QtkdDbContext db, int id)
    {
        var inDb = await db.Contracts.AsNoTracking().SingleAsync(c => c.Id == id);
        Assert.Equal(ContractStatus.Draft, inDb.Status);
        Assert.Null(inDb.SubmittedAt);
        Assert.Equal(0, await db.ContractHistories.CountAsync(h => h.ContractId == id && h.Action == "Submitted"));
    }
}
