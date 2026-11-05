# Kịch bản video demo Tuần 05

**Yêu cầu đề:** quay video không quá 5 phút: chạy API, đọc Customer/Contract, xem dữ liệu tương ứng trong PostgreSQL, và giải thích source code, cách xử lý, luồng hoạt động bằng cách hiểu của bản thân.

**Tổng thời lượng dự kiến:** 4 phút 45 giây.

> Lời thoại bên dưới chỉ là gợi ý. Hãy đọc lại bằng giọng của mình, không đọc nguyên văn.

---

## Bảng tổng quan

| Cảnh | Thời gian | Nội dung | Màn hình |
|---|---|---|---|
| 1 | 0:00 – 0:25 | Giới thiệu | VS Code |
| 2 | 0:25 – 1:05 | Cấu trúc solution | VS Code |
| 3 | 1:05 – 1:35 | Chạy API | PowerShell |
| 4 | 1:35 – 2:35 | Gọi API trên Swagger | Trình duyệt |
| 5 | 2:35 – 3:15 | Đối chiếu PostgreSQL | pgAdmin / DBeaver |
| 6 | 3:15 – 4:30 | Giải thích code và luồng | VS Code |
| 7 | 4:30 – 4:45 | Kết thúc | Bất kỳ |

---

## Chuẩn bị trước khi quay

- [ ] Mở sẵn 4 cửa sổ: VS Code, PowerShell, trình duyệt, pgAdmin hoặc DBeaver (đã kết nối database `qtkd`).
- [ ] Tắt API cũ cho trống cổng 5049:
  ```powershell
  Get-NetTCPConnection -LocalPort 5049 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
  ```
- [ ] Trong VS Code mở sẵn các tab theo thứ tự: `ContractsController.cs`, `ContractService.cs`, `QtkdDbContext.cs`, `ContractConfiguration.cs`, `DbSeeder.cs`.
- [ ] Tăng cỡ chữ cho dễ nhìn.
- [ ] Không để lộ mật khẩu: đừng mở `appsettings.Development.json` hay User Secrets.
- [ ] Chạy thử toàn bộ một lần, rồi tắt API trước khi quay.

---

## Cảnh 1: Giới thiệu (0:00 – 0:25)

**Màn hình:** VS Code, cây thư mục `src/`.

**Lời gợi ý:**
> Xin chào thầy/cô, em là [tên], MSSV [số]. Đây là bài thực hành tuần 05 đề tài Quản trị kinh doanh. Em dựng một Web API  lấy danh sách khách hàng và lấy hợp đồng theo id. Sau đây em chạy API, rồi đối chiếu dữ liệu trong PostgreSQL.

---

## Cảnh 2: Cấu trúc solution (0:25 – 1:05)

**Màn hình:** chỉ chuột vào 4 thư mục `QTKD.Domain`, `QTKD.Application`, `QTKD.Infrastructure`, `QTKD.Api`.

**Lời gợi ý:**
> Solution chia 4 lớp. **Domain** chứa entity như Customer, Contract và enum trạng thái, không phụ thuộc lớp nào. **Application** chứa interface, DTO và service xử lý nghiệp vụ. **Infrastructure** làm việc với database: DbContext, cấu hình bảng, migration và dữ liệu mẫu. **Api** là cửa vào HTTP gồm controller. Phụ thuộc đi vào trong, nên sau này đổi database cũng không phải sửa nghiệp vụ.

---

## Cảnh 3: Chạy API (1:05 – 1:35)

**Màn hình:** PowerShell.

```powershell
cd "F:\LAB_TH5,6,7\QTKD"
dotnet run --project src/QTKD.Api
```

Chờ thấy dòng `Now listening on: http://localhost:5049` và chỉ chuột vào dòng đó.

**Lời gợi ý:**
> Em chạy API bằng `dotnet run`. Khi khởi động, chương trình tự áp dụng migration để tạo bảng và nạp dữ liệu mẫu gồm 3 khách hàng, 2 hợp đồng. Chuỗi kết nối nằm trong User Secrets, không đưa lên Git để tránh lộ mật khẩu.

---

## Cảnh 4: Gọi API trên Swagger (1:35 – 2:35)

**Màn hình:** trình duyệt, `http://localhost:5049/swagger`.

| Thao tác | Chỉ vào chỗ nào |
|---|---|
| `GET /api/customers` → Try it out → Execute | 3 khách hàng, `id` 1, 2, 3; KH003 có `status: Suspended`, `taxCode: null` |
| `GET /api/contracts/{id}` với `1` | `contractNo: HD-2026-018`, `status: Draft`, `content: null`, `attachments: []` |
| `GET /api/contracts/{id}` với `2` | `HD-2026-019`, có `content`, có tệp `hop-dong-019.pdf` |
| `GET /api/contracts/{id}` với `999` | Mã **404** |

**Lời gợi ý:**
> Endpoint khách hàng trả 3 người, trạng thái Active hoặc Suspended. Hợp đồng số 1 là HD-2026-018 ở trạng thái Draft, thiếu nội dung và chưa có tệp đính kèm. Em cố ý để vậy để tuần 06 demo gửi duyệt thất bại. Hợp đồng số 2 đủ nội dung và có một tệp, dùng để demo gửi thành công. Khi gọi id không tồn tại, API trả 404 chứ không phải lỗi 500.

---

## Cảnh 5: Đối chiếu PostgreSQL (2:35 – 3:15)

**Màn hình:** pgAdmin hoặc DBeaver. Mở bảng `customers`, `contracts`, `contract_attachments`, hoặc chạy:

```sql
SELECT "Id", "Code", "Name", "Status" FROM customers ORDER BY "Id";
SELECT "Id", "ContractNo", "Status", "CustomerId" FROM contracts;
SELECT * FROM contract_attachments;
```

Nếu được, đặt cạnh kết quả Swagger và chỉ từng dòng khớp nhau.

**Lời gợi ý:**
> Em mở PostgreSQL để đối chiếu. Bảng customers có 3 dòng đúng như API trả. Bảng contracts có 2 hợp đồng đều Draft, `CustomerId` của hợp đồng 1 là 1, tức Công ty Minh Phát. Bảng contract_attachments có đúng một dòng gắn với hợp đồng số 2. Dữ liệu API trả về khớp hoàn toàn với database, chứng tỏ API đọc dữ liệu thật chứ không phải dữ liệu viết cứng.

**Kết quả đúng cần thấy:**

| API | PostgreSQL |
|---|---|
| 3 khách hàng, id 1–3 | `customers` có 3 dòng |
| Hợp đồng 1: Draft, không tệp | `contracts` dòng 1 là Draft, `contract_attachments` không có dòng của hợp đồng 1 |
| Hợp đồng 2: có `hop-dong-019.pdf` | `contract_attachments` có 1 dòng với `ContractId = 2` |

---

**Màn hình:** chỉ chuột vào 4 thư mục `QTKD.Domain`, `QTKD.Application`, `QTKD.Infrastructure`, `QTKD.Api`.


**Lời gợi ý:**
> Xin chào thầy/cô, em là [tên], MSSV [số]. Đây là bài thực hành tuần 05 đề tài Quản trị kinh doanh. Em dựng một Web API  lấy danh sách khách hàng và lấy hợp đồng theo id. Sau đây em chạy API, rồi đối chiếu dữ liệu trong PostgreSQL.




**********Lời gợi ý:**
> Solution chia 4 lớp. **Domain** chứa entity như Customer, Contract và enum trạng thái, không phụ thuộc lớp nào. **Application** chứa interface, DTO và service xử lý nghiệp vụ. **Infrastructure** làm việc với database: DbContext, cấu hình bảng, migration và dữ liệu mẫu. **Api** là cửa vào HTTP gồm controller. Phụ thuộc đi vào trong, nên sau này đổi database cũng không phải sửa nghiệp vụ.
## Cảnh 6: Giải thích code và luồng (3:15 – 4:30)



**Màn hình:** lần lượt từng file trong VS Code, chỉ chuột vào đúng dòng đang nói.

### 6.1. `ContractsController.cs`
> Request đi vào controller. Controller chỉ nhận id, gọi service, nếu kết quả null thì trả 404, ngược lại trả 200. Controller không chứa logic truy vấn.

### 6.2. `ContractService.cs`
> Service dùng LINQ tìm hợp đồng theo id, nối sang khách hàng và tệp đính kèm, rồi chuyển thành `ContractDto`. Em dùng `AsNoTracking` vì chỉ đọc. Em trả DTO thay vì entity để không lộ cấu trúc bảng.

### 6.3. `QtkdDbContext.cs` và `ContractConfiguration.cs`
> Service chỉ biết interface `IQtkdDbContext`. Thực tế EF Core dùng `QtkdDbContext` để dịch LINQ thành SQL gửi PostgreSQL. File Configuration mô tả bảng: số hợp đồng không được trùng, hợp đồng nối khóa ngoại với khách hàng.

### 6.4. `DbSeeder.cs` (lướt nhanh)
> Seeder nạp dữ liệu mẫu, và chỉ chạy khi bảng còn trống nên chạy lại nhiều lần không bị nhân đôi.

### 6.5. Chốt luồng (nói liền mạch)
> Tóm lại luồng là: Swagger gửi request, vào Controller, gọi Service, Service truy vấn qua DbContext, EF Core sinh SQL chạy trên PostgreSQL, kết quả đổ vào DTO rồi trả về JSON.

**Sơ đồ luồng để tham khảo:**

```
Swagger → Controller → Service → DbContext (EF Core) → PostgreSQL
                                                          │
Swagger ← JSON ← Controller ← DTO ← Service ←─────────────┘
```

---

## Cảnh 7: Kết thúc (4:30 – 4:45)

**Lời gợi ý:**
> Em đã chạy API, đọc khách hàng và hợp đồng, và đối chiếu khớp với PostgreSQL. Em xin cảm ơn thầy/cô đã xem.

---

## Mẹo để video đạt yêu cầu

- Đề nhấn mạnh **giải thích theo cách hiểu của em**. Cảnh 6 là phần quan trọng nhất: tự chọn vài ý bạn hiểu rõ và nói bằng lời của mình, đừng chỉ đọc kịch bản.
- Video phải thấy **cả API lẫn PostgreSQL**. Nếu được, chia đôi màn hình để dễ đối chiếu.
- Dưới 5 phút. Nếu nói chậm, cắt bớt cảnh 2.
- Không để lộ mật khẩu hay User Secrets trong khung hình.
- Quay thử một lần để canh giờ rồi quay chính thức.
- Nếu kết quả trên máy khác bảng trên (ví dụ id khác), hãy nói đúng theo cái thấy trên màn hình.

## Nếu gặp lỗi khi quay

| Lỗi | Cách xử lý |
|---|---|
| `address already in use` | Chạy lệnh tắt cổng 5049 ở phần Chuẩn bị, rồi chạy lại |
| `password authentication failed` | Đặt lại User Secrets, xem README mục 2 |
| `database "qtkd" does not exist` | `dotnet ef database update --project src/QTKD.Infrastructure --startup-project src/QTKD.Api` |
