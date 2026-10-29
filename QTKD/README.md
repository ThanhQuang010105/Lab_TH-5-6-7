# QTKD – Quản trị kinh doanh (Tuần 05)

Web API ASP.NET Core kết nối PostgreSQL, đọc dữ liệu thật từ database.

**Phạm vi Tuần 05:** dựng nền tảng (solution, entity, DbContext, migration, dữ liệu mẫu) và 2 endpoint đọc dữ liệu:

- `GET /api/customers`
- `GET /api/contracts/{id}`

Chưa gồm: tạo/sửa/gửi duyệt hợp đồng (Tuần 06), kiểm thử (Tuần 07), đăng nhập/phân quyền, giao diện web.

## 1. Yêu cầu cài đặt

| Công cụ | Phiên bản đã dùng |
|---|---|
| .NET SDK | 10.0.x (khóa trong `global.json`) |
| PostgreSQL | chạy ở `localhost:5432` |
| dotnet-ef | `dotnet tool install --global dotnet-ef` |
| Git | bản bất kỳ |

## 2. Cấu hình database

Chuỗi kết nối **không** nằm trong Git. Chọn một trong hai cách.

**Cách A – User Secrets (khuyến nghị):**

```powershell
cd "F:\LAB_TH5,6,7\QTKD"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=qtkd;Username=postgres;Password=MAT_KHAU_CUA_BAN" --project src/QTKD.Api
```

**Cách B – biến môi trường:**

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=qtkd;Username=postgres;Password=MAT_KHAU_CUA_BAN"
```

File mẫu: [src/QTKD.Api/appsettings.Example.json](src/QTKD.Api/appsettings.Example.json).

> Đường dẫn có dấu phẩy (`LAB_TH5,6,7`) phải đặt trong dấu nháy khi dùng PowerShell.

## 3. Chạy migration

```powershell
dotnet ef database update --project src/QTKD.Infrastructure --startup-project src/QTKD.Api
```

Lệnh tạo database `qtkd` cùng 6 bảng: `customers`, `contracts`, `contract_attachments`, `contract_histories`, `price_lists`, `price_items`.
Ở môi trường Development, API cũng tự migrate và nạp dữ liệu mẫu khi khởi động.

## 4. Khởi động API

```powershell
dotnet run --project src/QTKD.Api
```

Mở Swagger: <http://localhost:5049/swagger>

Nếu báo `address already in use`, tắt tiến trình đang giữ cổng:

```powershell
Get-NetTCPConnection -LocalPort 5049 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
```

## 5. Thử hai endpoint

Dữ liệu mẫu (seed): 3 khách hàng, 2 hợp đồng ở trạng thái Draft, 1 bảng giá có 2 dòng giá.

| Gọi | Kết quả đúng |
|---|---|
| `GET /api/customers` | 3 khách hàng: KH001, KH002, KH003 (id 1, 2, 3) |
| `GET /api/contracts/1` | HD-2026-018, `Draft`, `attachments: []` (thiếu nội dung và tệp) |
| `GET /api/contracts/2` | HD-2026-019, `Draft`, có tệp `hop-dong-019.pdf` (đủ điều kiện) |
| `GET /api/contracts/999` | HTTP 404 |

```powershell
curl.exe -s http://localhost:5049/api/customers
curl.exe -s http://localhost:5049/api/contracts/1
curl.exe -s http://localhost:5049/api/contracts/2
curl.exe -s -o NUL -w "%{http_code}" http://localhost:5049/api/contracts/999
```

Đối chiếu với PostgreSQL:

```sql
SELECT "Id", "Code", "Name", "Status" FROM customers ORDER BY "Id";
SELECT "Id", "ContractNo", "Status", "CustomerId" FROM contracts;
SELECT * FROM contract_attachments;
SELECT * FROM price_items;
```

## 6. Cấu trúc solution

Sơ đồ chi tiết: [docs/solution-overview.md](docs/solution-overview.md).

```
QTKD/
├── src/
│   ├── QTKD.Domain/          Entity, enum, exception nghiệp vụ (không phụ thuộc project nào)
│   ├── QTKD.Application/     Interface, DTO, service xử lý nghiệp vụ
│   ├── QTKD.Infrastructure/  EF Core, DbContext, cấu hình bảng, migration, seed
│   └── QTKD.Api/             Controller HTTP, Program.cs, Swagger, appsettings
├── tests/
│   ├── QTKD.UnitTests/       Unit test nghiệp vụ (Tuần 07)
│   └── QTKD.IntegrationTests/Test API + database kiểm thử riêng (Tuần 07)
├── docs/solution-overview.md
├── global.json
└── QTKD.slnx
```

| Project | Vai trò |
|---|---|
| QTKD.Domain | `Customer`, `Contract`, `ContractAttachment`, `ContractHistory`, `PriceList`, `PriceItem`; enum `ContractStatus`, `CustomerStatus`; `BusinessRuleException` |
| QTKD.Application | `ICustomerService`, `IContractService`, `IQtkdDbContext`, `CustomerDto`, `ContractDto` |
| QTKD.Infrastructure | `QtkdDbContext`, `Configurations/`, `Migrations/`, `DbSeeder` |
| QTKD.Api | `CustomersController`, `ContractsController`, `Program.cs` |

Quy tắc đã áp dụng: không đưa mật khẩu lên Git, không trả entity trực tiếp (luôn dùng DTO), cả solution dùng một phiên bản .NET.

## 7. Ghi chú cho các tuần sau

- Tuần 06: thêm chức năng vào `Application/Services` và `Api/Controllers`; quy tắc CTR-01..03 ném `BusinessRuleException`.
- Tuần 07: thêm test vào `tests/`, dùng `WebApplicationFactory<Program>` với database `qtkd_test`.
