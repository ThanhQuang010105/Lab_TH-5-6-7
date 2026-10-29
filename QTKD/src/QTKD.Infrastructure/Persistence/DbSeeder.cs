using Microsoft.EntityFrameworkCore;
using QTKD.Domain.Entities;
using QTKD.Domain.Enums;

namespace QTKD.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(QtkdDbContext db)
    {
        if (await db.Customers.AnyAsync()) return;   // chạy lại nhiều lần vẫn an toàn

        var minhPhat = new Customer
        {
            Code = "KH001", Name = "Công ty Minh Phát",
            TaxCode = "0301234567", Address = "Hải Phòng", Status = CustomerStatus.Active
        };
        var haiAu = new Customer
        {
            Code = "KH002", Name = "Công ty Hải Âu",
            TaxCode = "0307654321", Address = "Đà Nẵng", Status = CustomerStatus.Active
        };
        var tamNgung = new Customer
        {
            Code = "KH003", Name = "Công ty Tạm Ngưng",
            Status = CustomerStatus.Suspended      // dùng test "khách hàng không hợp lệ"
        };

        var now = DateTime.UtcNow;

        // HD-2026-018: THIẾU nội dung + tệp -> Tuần 06 demo gửi thất bại, bổ sung rồi gửi lại
        var hd018 = new Contract
        {
            ContractNo = "HD-2026-018", Customer = minhPhat,
            StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2027, 9, 30),
            ServiceGroup = "Dịch vụ cảng", Content = null, Status = ContractStatus.Draft
        };
        hd018.Histories.Add(new ContractHistory
        {
            Action = "Created", ToStatus = ContractStatus.Draft,
            PerformedBy = "seed", PerformedAt = now
        });

        // HD-2026-019: ĐỦ điều kiện -> test gửi thành công
        var hd019 = new Contract
        {
            ContractNo = "HD-2026-019", Customer = haiAu,
            StartDate = new DateOnly(2026, 11, 1), EndDate = new DateOnly(2027, 10, 31),
            ServiceGroup = "Dịch vụ kho bãi", Content = "Điều khoản dịch vụ lưu kho...",
            Status = ContractStatus.Draft
        };
        hd019.Attachments.Add(new ContractAttachment
        {
            FileName = "hop-dong-019.pdf", UploadedAt = now
        });
        hd019.Histories.Add(new ContractHistory
        {
            Action = "Created", ToStatus = ContractStatus.Draft,
            PerformedBy = "seed", PerformedAt = now
        });

        var priceList = new PriceList
        {
            Name = "Bảng giá dịch vụ cảng 2026",
            EffectiveDate = new DateOnly(2026, 10, 1),
            Items =
            {
                new PriceItem { ServiceCode = "CONT-HANDLING", UnitPrice = 850_000m },
                new PriceItem { ServiceCode = "STORAGE-DAY",   UnitPrice = 120_000m }
            }
        };

        db.Customers.AddRange(minhPhat, haiAu, tamNgung);
        db.Contracts.AddRange(hd018, hd019);
        db.PriceLists.Add(priceList);
        await db.SaveChangesAsync();
    }
}
