using Microsoft.EntityFrameworkCore;
using QTKD.Domain.Entities;
using QTKD.Domain.Enums;
using QTKD.Domain.Exceptions;
using QTKD.Infrastructure.Persistence;

namespace QTKD.UnitTests;

/// <summary>Tạo DbContext in-memory riêng cho từng test (không đụng PostgreSQL).</summary>
public static class TestHelpers
{
    public static QtkdDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<QtkdDbContext>()
            .UseInMemoryDatabase("qtkd-unit-" + Guid.NewGuid())
            .Options;
        return new QtkdDbContext(options);
    }

    public static async Task<Customer> AddCustomerAsync(
        QtkdDbContext db, CustomerStatus status = CustomerStatus.Active, string? code = null)
    {
        var c = new Customer
        {
            Code = code ?? "KH-" + Guid.NewGuid().ToString("N")[..6],
            Name = "Công ty Test",
            Status = status
        };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return c;
    }

    /// <summary>Tạo hợp đồng trực tiếp trong DB ở trạng thái tuỳ ý.</summary>
    public static async Task<Contract> AddContractAsync(
        QtkdDbContext db, Customer customer,
        ContractStatus status = ContractStatus.Draft,
        string? content = "Nội dung hợp đồng",
        bool withAttachment = false,
        DateOnly? start = null, DateOnly? end = null)
    {
        var c = new Contract
        {
            ContractNo = "HD-" + Guid.NewGuid().ToString("N")[..8],
            CustomerId = customer.Id,
            StartDate = start ?? new DateOnly(2026, 1, 1),
            EndDate = end ?? new DateOnly(2026, 12, 31),
            ServiceGroup = "Dịch vụ cảng",
            Content = content,
            Status = status
        };
        if (withAttachment)
            c.Attachments.Add(new ContractAttachment { FileName = "a.pdf", UploadedAt = DateTime.UtcNow });
        db.Contracts.Add(c);
        await db.SaveChangesAsync();
        return c;
    }

    /// <summary>Chạy action và trả về BusinessRuleException được ném ra (fail nếu không ném).</summary>
    public static async Task<BusinessRuleException> AssertRuleAsync(Func<Task> action)
        => await Assert.ThrowsAsync<BusinessRuleException>(action);
}
