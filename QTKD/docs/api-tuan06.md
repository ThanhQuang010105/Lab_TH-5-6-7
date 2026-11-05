# Tài liệu API — Tuần 06 QTKD

## Ánh xạ Business Rules → Code

| Rule | Ý nghĩa | File thực thi |
|---|---|---|
| CTR-01 | Chỉ sửa hợp đồng ở `Draft` hoặc `RevisionRequested` | [`ContractService.UpdateAsync`](../src/QTKD.Application/Services/ContractService.cs#L66-L91), [`AddAttachmentAsync`](../src/QTKD.Application/Services/ContractService.cs#L94-L120) |
| CTR-02 | Chỉ gửi khi Customer Active + ngày hiệu lực hợp lệ + có nội dung/tệp | [`ContractService.SubmitAsync`](../src/QTKD.Application/Services/ContractService.cs#L123-L175) |
| CTR-03 | Không chuyển Draft → Approved trực tiếp | [`ContractService.SubmitAsync`](../src/QTKD.Application/Services/ContractService.cs#L155-L170) — chỉ gán `PendingApproval` |

---

## 1. Customer API

### GET /api/customers
Lấy danh sách tất cả khách hàng (sắp xếp theo Code).

**Response 200 OK:**
```json
[
  { "id": 1, "code": "KH001", "name": "Công ty Minh Phát", "taxCode": "0301234567", "address": "Hải Phòng", "status": "Active" },
  { "id": 2, "code": "KH002", "name": "Công ty Hải Âu",   "taxCode": "0307654321", "address": "Đà Nẵng",   "status": "Active" }
]
```

---

### GET /api/customers/{id}
Lấy chi tiết một khách hàng.

**Response 200 OK:** Giống một phần tử trong danh sách trên.

**Response 404:** `{ "message": "Không tìm thấy khách hàng." }`

---

### POST /api/customers
Tạo khách hàng mới (mặc định `Status = Active`).

**Request Body:**
```json
{ "code": "KH004", "name": "Công ty An Bình", "taxCode": "0309999999", "address": "TP.HCM" }
```

**Response 201 Created:**
```json
{ "id": 4, "code": "KH004", "name": "Công ty An Bình", "taxCode": "0309999999", "address": "TP.HCM", "status": "Active" }
```

**Response 400 — Validation:**
```json
{ "code": "VALIDATION", "message": "Dữ liệu không hợp lệ.", "details": ["Code không được để trống."] }
```

**Response 400 — Trùng Code:**
```json
{ "code": "DUPLICATE_CODE", "message": "Mã khách hàng 'KH001' đã tồn tại.", "details": [] }
```

---

### PUT /api/customers/{id}
Cập nhật thông tin khách hàng.

**Request Body:**
```json
{ "name": "Công ty An Bình Mới", "taxCode": "0309999998", "address": "Hà Nội", "status": "Active" }
```

**Response 200 OK:** CustomerDto đã cập nhật.

**Response 404:** Không tìm thấy khách hàng.

---

## 2. Contract API

### GET /api/contracts/{id}
Lấy chi tiết hợp đồng kèm danh sách tệp đính kèm.

**Response 200 OK:**
```json
{
  "id": 1, "contractNo": "HD-2026-018",
  "customerId": 1, "customerName": "Công ty Minh Phát",
  "startDate": "2026-10-01", "endDate": "2027-09-30",
  "serviceGroup": "Dịch vụ cảng", "content": null,
  "status": "Draft", "submittedAt": null,
  "attachments": []
}
```

**Response 404:** `{ "message": "Không tìm thấy hợp đồng." }`

---

### POST /api/contracts
Tạo hợp đồng mới (Status mặc định `Draft`).

**Request Body:**
```json
{
  "contractNo": "HD-2026-020",
  "customerId": 1,
  "startDate": "2026-11-01",
  "endDate": "2027-10-31",
  "serviceGroup": "Dịch vụ vận tải",
  "content": null
}
```

**Response 201 Created:** ContractDto.

**Response 400 — Validation / Customer không hợp lệ:**
```json
{ "code": "CTR-02", "message": "...", "details": ["..."] }
```

---

### PUT /api/contracts/{id}
Cập nhật hợp đồng. **Vi phạm CTR-01 nếu status không phải Draft/RevisionRequested.**

**Request Body:**
```json
{
  "customerId": 1,
  "startDate": "2026-11-01",
  "endDate": "2027-10-31",
  "serviceGroup": "Dịch vụ cảng",
  "content": "Điều khoản dịch vụ cảng đầy đủ..."
}
```

**Response 200 OK:** ContractDto đã cập nhật.

**Response 409 — CTR-01:**
```json
{
  "code": "CTR-01",
  "message": "Vi phạm CTR-01: chỉ có thể cập nhật hợp đồng ở trạng thái Draft hoặc Revision Requested.",
  "details": ["Trạng thái hiện tại: PendingApproval"]
}
```

---

### POST /api/contracts/{id}/attachments
Thêm tệp đính kèm vào hợp đồng (cũng kiểm tra CTR-01).

**Request Body:**
```json
{ "fileName": "hop-dong-018.pdf" }
```

**Response 200 OK:**
```json
{ "id": 3, "fileName": "hop-dong-018.pdf" }
```

---

### POST /api/contracts/{id}/submit _(US03)_
Gửi hợp đồng trình duyệt. Kiểm tra **CTR-02** và đảm bảo **CTR-03**.

**Request Body:**
```json
{ "performedBy": "nguyen.van.a", "comment": "Gửi duyệt theo yêu cầu KH" }
```

**Response 200 OK — Thành công (CTR-02 đạt, CTR-03 đảm bảo):**
```json
{
  "id": 2, "contractNo": "HD-2026-019",
  "status": "PendingApproval",
  "submittedAt": "2026-11-05T02:55:00Z",
  ...
}
```

**Response 400 — CTR-02 không đạt:**
```json
{
  "code": "CTR-02",
  "message": "Hợp đồng chưa đáp ứng điều kiện gửi trình duyệt (CTR-02).",
  "details": [
    "Phải có nội dung hợp đồng (Content) hoặc ít nhất một tệp đính kèm."
  ]
}
```

**Response 409 — Sai trạng thái (không phải Draft/RevisionRequested):**
```json
{
  "code": "WRONG_STATE",
  "message": "Hợp đồng không ở trạng thái cho phép gửi trình duyệt.",
  "details": ["Trạng thái hiện tại: PendingApproval"]
}
```

---

## 3. PriceList API

### GET /api/pricelists
Lấy tất cả bảng giá (kèm danh sách đơn giá), sắp xếp giảm dần theo EffectiveDate.

**Response 200 OK:**
```json
[
  {
    "id": 1, "name": "Bảng giá dịch vụ cảng 2026",
    "effectiveDate": "2026-10-01",
    "items": [
      { "priceListId": 1, "serviceCode": "CONT-HANDLING", "unitPrice": 850000 },
      { "priceListId": 1, "serviceCode": "STORAGE-DAY",   "unitPrice": 120000 }
    ]
  }
]
```

---

### GET /api/pricelists/{id}
Lấy chi tiết một bảng giá.

---

### POST /api/pricelists
Tạo bảng giá mới.

**Request Body:**
```json
{ "name": "Bảng giá 2027", "effectiveDate": "2027-01-01" }
```

**Response 201 Created:** PriceListDto (Items rỗng).

---

### GET /api/pricelists/{id}/items
Lấy danh sách đơn giá của một bảng giá.

---

### POST /api/pricelists/{id}/items
Thêm đơn giá vào bảng giá.

**Request Body:**
```json
{ "serviceCode": "CRANE-HOUR", "unitPrice": 1500000 }
```

**Response 200 OK:**
```json
{ "priceListId": 1, "serviceCode": "CRANE-HOUR", "unitPrice": 1500000 }
```

**Response 400 — UnitPrice âm:**
```json
{ "code": "VALIDATION", "message": "Dữ liệu không hợp lệ.", "details": ["UnitPrice phải >= 0."] }
```

**Response 400 — Trùng ServiceCode:**
```json
{ "code": "DUPLICATE_CODE", "message": "ServiceCode 'CRANE-HOUR' đã tồn tại trong bảng giá này.", "details": [] }
```

---

## Kịch bản demo tuần 06

```
1.  POST /api/customers          → Tạo KH mới
2.  POST /api/contracts          → Tạo HD Draft (chưa có nội dung/tệp)
3.  POST /api/contracts/1/submit → 400 CTR-02 (thiếu nội dung & tệp)
4.  PUT  /api/contracts/1        → Bổ sung content
5.  POST /api/contracts/1/submit → 200 → Status: PendingApproval  ✓
6.  PUT  /api/contracts/1        → 409 CTR-01 (đang PendingApproval)
7.  Kiểm tra DB: contracts + contract_histories
```

