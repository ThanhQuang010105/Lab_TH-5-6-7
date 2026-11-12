using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Domain.Enums;

namespace QTKD.IntegrationTests;

/// <summary>TC01–TC08 qua HTTP thật + đối chiếu dữ liệu trong PostgreSQL (qtkd_test).</summary>
[Collection("api")]
public class ContractFlowTests(ApiFixture fx)
{
    private HttpClient Http => fx.Client;
    private static string Uniq(string p) => p + "-" + Guid.NewGuid().ToString("N")[..8];

    // ── helper dựng dữ liệu qua API ──
    private async Task<CustomerDto> CreateCustomerAsync()
    {
        var res = await Http.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest(Uniq("KH"), "Công ty Minh Phát", "0301234567", "Hải Phòng"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await ApiFixture.ReadAsync<CustomerDto>(res);
    }

    private async Task<ContractDto> CreateContractAsync(int customerId, string? content = null)
    {
        var res = await Http.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(Uniq("HD"), customerId,
                new DateOnly(2026, 10, 1), new DateOnly(2027, 9, 30), "Dịch vụ cảng", content));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await ApiFixture.ReadAsync<ContractDto>(res);
    }

    private static UpdateContractRequest Update(int customerId, string? content) =>
        new(customerId, new DateOnly(2026, 10, 1), new DateOnly(2027, 9, 30), "Dịch vụ cảng", content);

    private async Task<BusinessErrorResponse> ReadErrorAsync(HttpResponseMessage res) =>
        await ApiFixture.ReadAsync<BusinessErrorResponse>(res);

    // ───────────── TC01 ─────────────
    [Fact]
    public async Task TC01_CreateCustomer_Valid_201_AndSavedInDb()
    {
        var code = Uniq("KH");

        var res = await Http.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest(code, "Công ty Hải Âu", "0307654321", "Đà Nẵng"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        var body = await ApiFixture.ReadAsync<CustomerDto>(res);
        Assert.Equal(code, body.Code);
        Assert.Equal(CustomerStatus.Active, body.Status);

        // đọc lại qua API
        var get = await Http.GetAsync($"/api/customers/{body.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        // đối chiếu trong DB
        using var db = fx.NewDb();
        var row = await db.Customers.AsNoTracking().SingleAsync(c => c.Id == body.Id);
        Assert.Equal("Công ty Hải Âu", row.Name);
        Assert.Equal(CustomerStatus.Active, row.Status);
    }

    // ───────────── TC02 ─────────────
    [Fact]
    public async Task TC02_CreateCustomer_MissingRequired_400_WithMessage_NothingSaved()
    {
        using var db = fx.NewDb();
        var before = await db.Customers.CountAsync();

        var res = await Http.PostAsJsonAsync("/api/customers", new CreateCustomerRequest("", "", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var err = await ReadErrorAsync(res);
        Assert.Equal("VALIDATION", err.Code);
        Assert.Equal(2, err.Details.Count);                     // thiếu Code và Name
        Assert.Equal(before, await db.Customers.CountAsync());   // không lưu gì
    }

    [Fact]
    public async Task CreateCustomer_DuplicateCode_400()
    {
        var c = await CreateCustomerAsync();

        var res = await Http.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(c.Code, "Khác", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("DUPLICATE_CODE", (await ReadErrorAsync(res)).Code);
    }

    // ───────────── TC03 ─────────────
    [Fact]
    public async Task TC03_UpdateContract_Draft_200_AndPersisted()
    {
        var c = await CreateCustomerAsync();
        var ct = await CreateContractAsync(c.Id);

        var res = await Http.PutAsJsonAsync($"/api/contracts/{ct.Id}", Update(c.Id, "Điều khoản mới"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await ApiFixture.ReadAsync<ContractDto>(res);
        Assert.Equal("Điều khoản mới", body.Content);
        Assert.Equal(ContractStatus.Draft, body.Status);
        using var db = fx.NewDb();
        Assert.Equal("Điều khoản mới", (await db.Contracts.AsNoTracking().SingleAsync(x => x.Id == ct.Id)).Content);
    }

    // ───────────── TC04 ─────────────
    [Fact]
    public async Task TC04_UpdateContract_PendingApproval_409_DataUnchanged()
    {
        var c = await CreateCustomerAsync();
        var ct = await CreateContractAsync(c.Id, content: "Nội dung gốc");
        Assert.Equal(HttpStatusCode.OK, (await Http.PostAsJsonAsync($"/api/contracts/{ct.Id}/submit",
            new SubmitContractRequest("sales01", null))).StatusCode);

        var res = await Http.PutAsJsonAsync($"/api/contracts/{ct.Id}", Update(c.Id, "Cố tình sửa"));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("CTR-01", (await ReadErrorAsync(res)).Code);
        using var db = fx.NewDb();
        var row = await db.Contracts.AsNoTracking().SingleAsync(x => x.Id == ct.Id);
        Assert.Equal("Nội dung gốc", row.Content);               // dữ liệu giữ nguyên
        Assert.Equal(ContractStatus.PendingApproval, row.Status);
    }

    // ───────────── TC05 ─────────────
    [Fact]
    public async Task TC05_Submit_MissingCtr02_400_StaysDraft_NoHistory()
    {
        var c = await CreateCustomerAsync();
        var ct = await CreateContractAsync(c.Id, content: null);          // thiếu nội dung và tệp

        var res = await Http.PostAsJsonAsync($"/api/contracts/{ct.Id}/submit", new SubmitContractRequest("sales01", null));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var err = await ReadErrorAsync(res);
        Assert.Equal("CTR-02", err.Code);
        Assert.NotEmpty(err.Details);
        using var db = fx.NewDb();
        var row = await db.Contracts.AsNoTracking().SingleAsync(x => x.Id == ct.Id);
        Assert.Equal(ContractStatus.Draft, row.Status);
        Assert.Null(row.SubmittedAt);
        Assert.Equal(0, await db.ContractHistories.CountAsync(h => h.ContractId == ct.Id && h.Action == "Submitted"));
    }

    // ───────────── TC06 ─────────────
    [Fact]
    public async Task TC06_Submit_Valid_200_PendingApproval_HistoryWritten()
    {
        var c = await CreateCustomerAsync();
        var ct = await CreateContractAsync(c.Id, content: "Điều khoản dịch vụ");

        var res = await Http.PostAsJsonAsync($"/api/contracts/{ct.Id}/submit", new SubmitContractRequest("sales01", "Gửi duyệt"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await ApiFixture.ReadAsync<ContractDto>(res);
        Assert.Equal(ContractStatus.PendingApproval, body.Status);
        Assert.NotNull(body.SubmittedAt);

        // đọc lại bằng GET
        var again = await ApiFixture.ReadAsync<ContractDto>(await Http.GetAsync($"/api/contracts/{ct.Id}"));
        Assert.Equal(ContractStatus.PendingApproval, again.Status);
        // đối chiếu DB (trạng thái lưu dạng chuỗi) + lịch sử
        using var db = fx.NewDb();
        var row = await db.Contracts.AsNoTracking().SingleAsync(x => x.Id == ct.Id);
        Assert.Equal(ContractStatus.PendingApproval, row.Status);
        var h = await db.ContractHistories.AsNoTracking().SingleAsync(x => x.ContractId == ct.Id && x.Action == "Submitted");
        Assert.Equal(ContractStatus.Draft, h.FromStatus);
        Assert.Equal(ContractStatus.PendingApproval, h.ToStatus);
        Assert.Equal("sales01", h.PerformedBy);
    }

    // ───────────── TC07 ─────────────
    [Fact]
    public async Task TC07_SubmitNeverProducesApproved_AndNoEndpointToForceApproved()
    {
        var c = await CreateCustomerAsync();
        var ct = await CreateContractAsync(c.Id, content: "ok");

        var res = await Http.PostAsJsonAsync($"/api/contracts/{ct.Id}/submit", new SubmitContractRequest(null, null));
        Assert.Equal(ContractStatus.PendingApproval, (await ApiFixture.ReadAsync<ContractDto>(res)).Status);

        // Không có đường nào đặt Status = Approved qua API: PUT bỏ qua trường status
        var put = await Http.PostAsJsonAsync($"/api/contracts/{ct.Id}/approve", new { });
        Assert.True(put.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);

        using var db = fx.NewDb();
        Assert.NotEqual(ContractStatus.Approved, (await db.Contracts.AsNoTracking().SingleAsync(x => x.Id == ct.Id)).Status);
    }

    [Fact]
    public async Task TC07_RuleEngine_DraftToApproved_Rejected_CTR03()
    {
        var ex = Assert.Throws<QTKD.Domain.Exceptions.BusinessRuleException>(() =>
            QTKD.Domain.Rules.ContractTransitions.EnsureCanTransition(ContractStatus.Draft, ContractStatus.Approved));
        Assert.Equal("CTR-03", ex.Code);
        await Task.CompletedTask;
    }

    // ───────────── TC08 ─────────────
    [Fact]
    public async Task TC08_SubmitTwice_SecondIs409_NoDuplicateHistory_SubmittedAtUnchanged()
    {
        var c = await CreateCustomerAsync();
        var ct = await CreateContractAsync(c.Id, content: "ok");
        var url = $"/api/contracts/{ct.Id}/submit";

        var first = await Http.PostAsJsonAsync(url, new SubmitContractRequest("sales01", null));
        var firstBody = await ApiFixture.ReadAsync<ContractDto>(first);
        var second = await Http.PostAsJsonAsync(url, new SubmitContractRequest("sales01", null));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("WRONG_STATE", (await ReadErrorAsync(second)).Code);
        using var db = fx.NewDb();
        Assert.Equal(1, await db.ContractHistories.CountAsync(h => h.ContractId == ct.Id && h.Action == "Submitted"));
        var row = await db.Contracts.AsNoTracking().SingleAsync(x => x.Id == ct.Id);
        // PostgreSQL chỉ lưu đến microsecond nên so sánh có dung sai 1ms
        Assert.NotNull(row.SubmittedAt);
        Assert.True(Math.Abs((firstBody.SubmittedAt!.Value - row.SubmittedAt!.Value).TotalMilliseconds) < 1);
    }

    // ───────────── Luồng demo tuần 06 (end-to-end) ─────────────
    [Fact]
    public async Task Demo_Week06_Flow_Customer_Contract_FailSubmit_Fix_SubmitOk()
    {
        var c = await CreateCustomerAsync();
        var ct = await CreateContractAsync(c.Id, content: null);
        var url = $"/api/contracts/{ct.Id}/submit";

        var fail = await Http.PostAsJsonAsync(url, new SubmitContractRequest("sales01", null));
        Assert.Equal(HttpStatusCode.BadRequest, fail.StatusCode);

        var att = await Http.PostAsJsonAsync($"/api/contracts/{ct.Id}/attachments", new AddAttachmentRequest("hop-dong.pdf"));
        Assert.Equal(HttpStatusCode.Created, att.StatusCode);

        var ok = await Http.PostAsJsonAsync(url, new SubmitContractRequest("sales01", null));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ApiFixture.ReadAsync<ContractDto>(ok);
        Assert.Equal(ContractStatus.PendingApproval, body.Status);
        Assert.Single(body.Attachments);
    }

    // ───────────── Đọc dữ liệu seed (tuần 05) ─────────────
    [Fact]
    public async Task Seed_GetContract999_404_And_Customers_ContainSeed()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync("/api/contracts/999999")).StatusCode);

        var list = await ApiFixture.ReadAsync<List<CustomerDto>>(await Http.GetAsync("/api/customers"));
        Assert.Contains(list, x => x.Code == "KH001");
        Assert.Contains(list, x => x.Code == "KH003" && x.Status == CustomerStatus.Suspended);
    }

    [Fact]
    public async Task Submit_SuspendedSeedCustomerContract_Rejected()
    {
        // KH003 (Suspended) không thể có hợp đồng mới
        using var db = fx.NewDb();
        var suspended = await db.Customers.AsNoTracking().SingleAsync(x => x.Code == "KH003");

        var res = await Http.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(Uniq("HD"), suspended.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null, "x"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("CUSTOMER_INACTIVE", (await ReadErrorAsync(res)).Code);
    }
}
