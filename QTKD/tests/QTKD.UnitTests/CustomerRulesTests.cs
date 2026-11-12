using Microsoft.EntityFrameworkCore;
using QTKD.Application.DTOs;
using QTKD.Application.Services;
using QTKD.Domain.Enums;
using static QTKD.UnitTests.TestHelpers;

namespace QTKD.UnitTests;

public class CustomerRulesTests
{
    [Fact]   // TC01
    public async Task TC01_Create_Valid_SavedAndReadBack()
    {
        using var db = NewDb();
        var svc = new CustomerService(db);

        var created = await svc.CreateAsync(new CreateCustomerRequest("KH100", "  Công ty Minh Phát ", "0301234567", "Hải Phòng"));

        var read = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(read);
        Assert.Equal("KH100", read!.Code);
        Assert.Equal("Công ty Minh Phát", read.Name);           // đã Trim
        Assert.Equal(CustomerStatus.Active, read.Status);
    }

    [Theory]   // TC02
    [InlineData("", "Tên")]
    [InlineData("KH1", "")]
    [InlineData("  ", "   ")]
    public async Task TC02_Create_MissingRequired_Rejected(string code, string name)
    {
        using var db = NewDb();
        var svc = new CustomerService(db);

        var ex = await AssertRuleAsync(() => svc.CreateAsync(new CreateCustomerRequest(code, name, null, null)));

        Assert.Equal("VALIDATION", ex.Code);
        Assert.NotEmpty(ex.Details);
        Assert.Equal(0, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task Create_DuplicateCode_Rejected()
    {
        using var db = NewDb();
        var svc = new CustomerService(db);
        await svc.CreateAsync(new CreateCustomerRequest("KH1", "A", null, null));

        var ex = await AssertRuleAsync(() => svc.CreateAsync(new CreateCustomerRequest("KH1", "B", null, null)));

        Assert.Equal("DUPLICATE_CODE", ex.Code);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNull()
    {
        using var db = NewDb();
        var svc = new CustomerService(db);

        Assert.Null(await svc.UpdateAsync(123, new UpdateCustomerRequest("A", null, null, CustomerStatus.Active)));
    }

    [Fact]
    public async Task Update_EmptyName_Rejected_DataUnchanged()
    {
        using var db = NewDb();
        var svc = new CustomerService(db);
        var c = await svc.CreateAsync(new CreateCustomerRequest("KH1", "Gốc", null, null));

        var ex = await AssertRuleAsync(() => svc.UpdateAsync(c.Id, new UpdateCustomerRequest(" ", null, null, CustomerStatus.Suspended)));

        Assert.Equal("VALIDATION", ex.Code);
        var inDb = await db.Customers.AsNoTracking().SingleAsync();
        Assert.Equal("Gốc", inDb.Name);
        Assert.Equal(CustomerStatus.Active, inDb.Status);
    }
}
