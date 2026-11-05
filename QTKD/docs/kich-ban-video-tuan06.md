# BÁO CÁO THỰC HÀNH TUẦN 06 & KỊCH BẢN QUAY VIDEO DEMO (5 PHÚT)

> **Môn học:** Phát triển ứng dụng hệ thống thông tin hiện đại  
> **Đề tài:** Quản trị kinh doanh (QTKD) — Phase 2: Tuần 06  
> **Trọng tâm:** User Story US03 (Gửi hợp đồng trình duyệt) & Các quy tắc CTR-01, CTR-02, CTR-03

---

## MỤC LỤC
1. [Danh sách các file đã tạo mới / chỉnh sửa trong Tuần 06](#1-danh-sách-các-file-đã-tạo-mới--chỉnh-sửa-trong-tuần-06)
2. [Bảng đối chiếu Business Rules với Source Code](#2-bảng-đối-chiếu-business-rules-với-source-code)
3. [Kịch bản chi tiết quay Video Demo 5 phút](#3-kịch-bản-chi-tiết-quay-video-demo-5-phút)
4. [Tài liệu API chi tiết (Request/Response mẫu copy-paste)](#4-tài-liệu-api-chi-tiết-requestresponse-mẫu)

---
## 2. BẢNG ĐỐI CHIẾU BUSINESS RULES VỚI SOURCE CODE

| Mã quy tắc / US | Nội dung quy tắc | Vị trí code xử lý cụ thể | Mã HTTP trả về |
|---|---|---|---|
| **CTR-01** | Chỉ cho phép chỉnh sửa hợp đồng hoặc thêm tệp khi hợp đồng ở trạng thái `Draft` hoặc `RevisionRequested`. Cố tình sửa khi ở trạng thái khác (ví dụ `PendingApproval`) sẽ bị từ chối. | `ContractService.cs` trong hàm `UpdateAsync` (dòng 66-74) và `AddAttachmentAsync` (dòng 94-102) | **`409 Conflict`** |
| **CTR-02** | Hợp đồng chỉ được Submit khi:<br>1. Khách hàng tồn tại và `Active`.<br>2. Ngày hiệu lực hợp lệ (`StartDate <= EndDate`).<br>3. Có nội dung (`Content`) HOẶC có ít nhất 1 tệp đính kèm. | `ContractService.cs` trong hàm `SubmitAsync` (dòng 125-148) | **`400 Bad Request`** (kèm mảng `details` lỗi) |
| **CTR-03** | Không được chuyển trực tiếp từ `Draft` sang `Approved`. Submit thành công chỉ chuyển sang `PendingApproval`. | `ContractService.cs` trong hàm `SubmitAsync` (dòng 150-153) | **`200 OK`** |
| **US03** | Nhân viên Kinh doanh gửi hợp đồng trình duyệt; ghi nhận vết lịch sử xử lý vào database. | `ContractService.cs` trong hàm `SubmitAsync` (dòng 155-165) thêm bản ghi vào `contract.Histories` | **`200 OK`** |

---

## 3. KỊCH BẢN CHI TIẾT QUAY VIDEO DEMO (5 PHÚT)

### 準備 (Chuẩn bị trước khi bấm quay):
1. Chạy API: `dotnet run --project src/QTKD.Api`
2. Mở trình duyệt vào Swagger: `http://localhost:5049/swagger`
3. Mở sẵn file `ContractService.cs` trong VS Code.
4. Mở công cụ Database (DBeaver / pgAdmin / hoặc PowerShell psql).

---


## 1. DANH SÁCH CÁC FILE ĐÃ TẠO MỚI / CHỈNH SỬA TRONG TUẦN 06


```
src/
├── QTKD.Application/                 (Tầng xử lý logic nghiệp vụ)
│   ├── DTOs/
│   │   ├── CustomerDto.cs           [CẬP NHẬT] Thêm CreateCustomerRequest, UpdateCustomerRequest
│   │   └── ContractDto.cs           [CẬP NHẬT] Thêm Request cho Contract, PriceList và BusinessErrorResponse
│   ├── Interfaces/
│   │   ├── ICustomerService.cs      [CẬP NHẬT] Khai báo CreateAsync, UpdateAsync, GetByIdAsync
│   │   ├── IContractService.cs      [CẬP NHẬT] Khai báo CreateAsync, UpdateAsync, AddAttachmentAsync, SubmitAsync
│   │   └── IPriceListService.cs     [TẠO MỚI]  Giao diện quản lý bảng giá và dòng đơn giá
│   ├── Services/
│   │   ├── CustomerService.cs       [CẬP NHẬT] Xử lý kiểm tra trùng Code, khách hàng Active
│   │   ├── ContractService.cs       [CẬP NHẬT] NƠI THỰC THI QUY TẮC CTR-01, CTR-02, CTR-03 & US03
│   │   └── PriceListService.cs      [TẠO MỚI]  Xử lý thêm bảng giá, kiểm tra UnitPrice >= 0
│   └── DependencyInjection.cs       [CẬP NHẬT] Đăng ký IPriceListService vào Service Container
│
└── QTKD.Api/                         (Tầng giao diện Web API)
    ├── Controllers/
    │   ├── CustomersController.cs   [CẬP NHẬT] Thêm POST tạo, PUT sửa, GET chi tiết khách hàng
    │   ├── ContractsController.cs   [CẬP NHẬT] Thêm POST tạo, PUT sửa, POST tệp, POST submit (US03)
    │   └── PriceListsController.cs  [TẠO MỚI]  Controller cho bảng giá và đơn giá dịch vụ
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs [TẠO MỚI] Bắt BusinessRuleException trả về HTTP 400/409 chuẩn
    └── Program.cs                   [CẬP NHẬT] Kích hoạt ExceptionHandlingMiddleware
```

---



### ⏱ PHÚT 0:00 – 1:00: GIỚI THIỆU TỔNG QUAN & SOURCE CODE
- **Màn hình:** Quay màn hình VS Code, mở file `src/QTKD.Application/Services/ContractService.cs`.
- **Thuyết minh:**
  > *"Kính chào thầy/cô, hôm nay em xin demo kết quả bài thực hành Tuần 06 môn Phát triển ứng dụng HTTT hiện đại.
  > Đề tài của nhóm là Quản trị kinh doanh (QTKD). Trong tuần này, nhóm tập trung hiện thực User Story US03: Gửi hợp đồng trình duyệt cùng 3 quy tắc nghiệp vụ CTR-01, CTR-02 và CTR-03.
  > Về cấu trúc mã nguồn:
  > - Tầng Application đảm nhận toàn bộ nghiệp vụ. Cụ thể trong `ContractService.cs`:
  >   + Hàm `UpdateAsync` kiểm tra CTR-01: chỉ cho phép cập nhật khi hợp đồng ở Draft hoặc RevisionRequested.
  >   + Hàm `SubmitAsync` kiểm tra CTR-02: kiểm tra trạng thái khách hàng Active, ngày hiệu lực và bắt buộc phải có nội dung hoặc tài liệu đính kèm.
  >   + Khi submit hợp lệ, trạng thái chỉ chuyển sang PendingApproval mà không được duyệt thẳng sang Approved, tuân thủ CTR-03. Đồng thời lưu vết lịch sử vào bảng contract_histories.
  > - Ngoài ra nhóm có xây dựng `ExceptionHandlingMiddleware` để bắt các lỗi nghiệp vụ và trả về HTTP status 400 hoặc 409 chuẩn RESTful."*

---

### ⏱ PHÚT 1:00 – 2:00: TẠO KHÁCH HÀNG & HỢP ĐỒNG NHÁP
- **Màn hình:** Chuyển sang trình duyệt Swagger.
- **Thao tác 1:** Mở `POST /api/customers` $\rightarrow$ Bấm **Try it out** $\rightarrow$ Dán JSON:
  ```json
  {
    "code": "KH004",
    "name": "Công ty TNHH Vận Tải Biển Đông",
    "taxCode": "0312345678",
    "address": "TP. Hồ Chí Minh"
  }
  ```
  $\rightarrow$ Bấm **Execute** $\rightarrow$ Kết quả: **`201 Created`** (Khách hàng có `id = 4`, `status = Active`).
- **Thuyết minh:**
  > *"Trước tiên, nhân viên kinh doanh tạo một khách hàng mới. Khách hàng này có trạng thái mặc định là Active."*

- **Thao tác 2:** Mở `POST /api/contracts` $\rightarrow$ Bấm **Try it out** $\rightarrow$ Tạo hợp đồng nhưng cố tình **để trống `content: null`**:
  ```json
  {
    "contractNo": "HD-2026-020",
    "customerId": 4,
    "startDate": "2026-11-01",
    "endDate": "2027-10-31",
    "serviceGroup": "Dịch vụ kho bãi",
    "content": null
  }
  ```
  $\rightarrow$ Bấm **Execute** $\rightarrow$ Kết quả: **`201 Created`** (Hợp đồng có `id = 3`, `status = Draft`, `attachments` rỗng).
- **Thuyết minh:**
  > *"Sau đó tạo hợp đồng số HD-2026-020 ở trạng thái Draft. Hợp đồng này hiện tại chưa có nội dung và chưa có tệp đính kèm nào."*

---

### ⏱ PHÚT 2:00 – 3:15: DEMO TRƯỜNG HỢP THẤT BẠI (KIỂM CHỨNG CTR-02)
- **Màn hình:** Mở API `POST /api/contracts/{id}/submit`.
- **Thao tác:**
  - Nhập `id`: `3` (hợp đồng vừa tạo, hoặc nhập `1` là hợp đồng mẫu HD-2026-018 có sẵn).
  - Body:
    ```json
    {
      "performedBy": "Nguyen Van A",
      "comment": "Trình duyệt hợp đồng mới"
    }
    ```
  - Bấm **Execute**.
- **Kết quả hiển thị:** Mã **`400 Bad Request`**
  ```json
  {
    "code": "CTR-02",
    "message": "Hợp đồng chưa đáp ứng điều kiện gửi trình duyệt (CTR-02).",
    "details": [
      "Phải có nội dung hợp đồng (Content) hoặc ít nhất một tệp đính kèm."
    ]
  }
  ```
- **Thuyết minh:**
  > *"Bây giờ nhân viên gửi trình duyệt khi hồ sơ chưa đầy đủ. Ngay lập tức quy tắc CTR-02 phát hiện vi phạm: hệ thống từ chối với mã lỗi 400 Bad Request và chỉ rõ lý do: thiếu nội dung hoặc tệp đính kèm. Hợp đồng trong database vẫn giữ nguyên trạng thái Draft."*

---

### ⏱ PHÚT 3:15 – 4:00: BỔ SUNG TỆP & GỬI DUYỆT THÀNH CÔNG (CTR-02 & CTR-03)
- **Màn hình:** Mở API `POST /api/contracts/{id}/attachments`.
- **Thao tác 1 (Bổ sung tệp):**
  - `id`: `3`
  - Body:
    ```json
    {
      "fileName": "hop-dong-kinh-doanh-020.pdf"
    }
    ```
  - Bấm **Execute** $\rightarrow$ Trả về **`200 OK`**.
- **Thuyết minh:**
  > *"Nhân viên bổ sung tài liệu đính kèm file PDF cho hợp đồng."*

- **Thao tác 2 (Gửi duyệt lại):**
  - Quay lại API `POST /api/contracts/{id}/submit` (với `id: 3`).
  - Bấm **Execute** lại.
- **Kết quả hiển thị:** Trả về **`200 OK`**
  - `"status": "PendingApproval"`
  - `"submittedAt": "2026-11-05T..."`
- **Thuyết minh:**
  > *"Khi bấm gửi duyệt lại, hợp đồng đã đáp ứng đủ điều kiện CTR-02. Trạng thái hợp đồng chuyển thành PendingApproval theo đúng CTR-03, và thời gian submittedAt được ghi nhận."*

---

### ⏱ PHÚT 4:00 – 4:40: DEMO TỪ CHỐI SỬA HỢP ĐỒNG (KIỂM CHỨNG CTR-01)
- **Màn hình:** Mở API `PUT /api/contracts/{id}`.
- **Thao tác:**
  - Nhập `id`: `3` (lúc này hợp đồng 3 đang ở `PendingApproval`).
  - Dán body cố tình chỉnh sửa ngày/nội dung:
    ```json
    {
      "customerId": 4,
      "startDate": "2026-11-01",
      "endDate": "2027-10-31",
      "serviceGroup": "Dịch vụ kho bãi",
      "content": "Cố tình sửa nội dung khi đang chờ duyệt"
    }
    ```
  - Bấm **Execute**.
- **Kết quả hiển thị:** Trả về **`409 Conflict`**
  ```json
  {
    "code": "CTR-01",
    "message": "Vi phạm CTR-01: chỉ có thể cập nhật hợp đồng ở trạng thái Draft hoặc Revision Requested.",
    "details": [
      "Trạng thái hiện tại: PendingApproval"
    ]
  }
  ```
- **Thuyết minh:**
  > *"Để kiểm tra quy tắc CTR-01: Khi hợp đồng đang chờ duyệt, nếu có người cố tình gửi yêu cầu sửa đổi, hệ thống sẽ chặn lại và báo lỗi 409 Conflict. Hợp đồng chỉ được phép sửa khi đang ở bản nháp Draft hoặc khi cấp trên yêu cầu chỉnh sửa lại (RevisionRequested)."*

---

### ⏱ PHÚT 4:40 – 5:00: ĐỐI CHIẾU DỮ LIỆU DATABASE & KẾT THÚC
- **Màn hình:** Mở DBeaver / pgAdmin hoặc Terminal câu lệnh SQL:
  ```sql
  SELECT "Id", "ContractNo", "Status", "SubmittedAt" FROM contracts WHERE "Id" = 3;
  SELECT * FROM contract_histories WHERE "ContractId" = 3;
  ```
- **Thuyết minh:**
  > *"Đối chiếu trong PostgreSQL: Bảng contracts đã lưu đúng trạng thái PendingApproval và bảng contract_histories đã ghi vết lịch sử hành động Submitted. Toàn bộ luồng nghiệp vụ hoạt động chính xác theo yêu cầu. Em xin kết thúc phần trình bày, cảm ơn thầy cô!"*

---

## 4. TÀI LIỆU API CHI TIẾT (REQUEST/RESPONSE MẪU)

### 4.1. Khách hàng (Customer)
- **GET** `/api/customers`: Lấy danh sách tất cả khách hàng.
- **GET** `/api/customers/{id}`: Xem chi tiết 1 khách hàng.
- **POST** `/api/customers`: Tạo khách hàng mới.
  - Body:
    ```json
    { "code": "KH004", "name": "Công ty TNHH Vận Tải Biển Đông", "taxCode": "0312345678", "address": "TP. Hồ Chí Minh" }
    ```
- **PUT** `/api/customers/{id}`: Sửa khách hàng.
  - Body:
    ```json
    { "name": "Công ty Biển Đông Cập Nhật", "taxCode": "0312345678", "address": "Hà Nội", "status": "Active" }
    ```

### 4.2. Hợp đồng (Contract)
- **GET** `/api/contracts/{id}`: Xem chi tiết hợp đồng và danh sách tệp đính kèm.
- **POST** `/api/contracts`: Tạo hợp đồng nháp.
  - Body:
    ```json
    { "contractNo": "HD-2026-020", "customerId": 1, "startDate": "2026-11-01", "endDate": "2027-10-31", "serviceGroup": "Dịch vụ cảng", "content": null }
    ```
- **PUT** `/api/contracts/{id}`: Chỉnh sửa hợp đồng (kiểm tra CTR-01).
- **POST** `/api/contracts/{id}/attachments`: Thêm tệp đính kèm cho hợp đồng.
  - Body:
    ```json
    { "fileName": "hop-dong-scan.pdf" }
    ```
- **POST** `/api/contracts/{id}/submit`: **US03 - Gửi hợp đồng trình duyệt**.
  - Body:
    ```json
    { "performedBy": "Nguyen Van A", "comment": "Trình duyệt hợp đồng" }
    ```

### 4.3. Bảng giá (PriceList & PriceItem)
- **GET** `/api/pricelists`: Lấy tất cả bảng giá kèm danh sách các dòng đơn giá.
- **POST** `/api/pricelists`: Tạo bảng giá mới.
  - Body:
    ```json
    { "name": "Bảng giá cảng 2027", "effectiveDate": "2027-01-01" }
    ```
- **POST** `/api/pricelists/{id}/items`: Thêm dòng đơn giá vào bảng giá.
  - Body:
    ```json
    { "serviceCode": "CRANE-HOUR", "unitPrice": 1500000 }
    ```

