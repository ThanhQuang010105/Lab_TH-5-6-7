# Báo cáo kiểm thử Tuần 07

Kết quả `dotnet test` (đã lưu file TRX tại [docs/test-results/](test-results/)):

| Project | Loại | Số test | Kết quả |
|---|---|---|---|
| QTKD.UnitTests | Unit test nghiệp vụ (EF Core InMemory, không cần PostgreSQL) | 36 | 36 Pass |
| QTKD.IntegrationTests | Gọi API thật + đối chiếu PostgreSQL `qtkd_test` | 13 | 13 Pass |

## Bảng TC01–TC08

| Test | Thao tác | Kết quả mong đợi | Unit test (tự động) | Integration test (tự động) | Kết quả |
|---|---|---|---|---|---|
| TC01 | Tạo Customer hợp lệ | Tạo được, đọc lại từ DB | `CustomerRulesTests.TC01_*` | `TC01_CreateCustomer_Valid_201_AndSavedInDb` | Pass |
| TC02 | Tạo Customer thiếu thông tin | Bị từ chối, có lỗi | `CustomerRulesTests.TC02_*` (3 ca) | `TC02_CreateCustomer_MissingRequired_400_*` | Pass |
| TC03 | Sửa Contract ở Draft | Thành công | `CTR01_Update_AllowedStatuses_Succeeds` (Draft, RevisionRequested) | `TC03_UpdateContract_Draft_200_AndPersisted` | Pass |
| TC04 | Sửa Contract ở PendingApproval | Bị từ chối, dữ liệu giữ nguyên | `CTR01_Update_OtherStatuses_RejectedAndDataUnchanged` (4 trạng thái) | `TC04_UpdateContract_PendingApproval_409_DataUnchanged` | Pass |
| TC05 | Submit thiếu điều kiện CTR-02 | Bị từ chối, vẫn Draft | `CTR02_Submit_*` (thiếu nội dung/tệp, khách Suspended, ngày sai, nhiều lỗi) | `TC05_Submit_MissingCtr02_400_StaysDraft_NoHistory` | Pass |
| TC06 | Submit hợp lệ | Sang PendingApproval | `CTR02_Submit_WithContentOnly/WithAttachmentOnly_Succeeds` | `TC06_Submit_Valid_200_PendingApproval_HistoryWritten` | Pass |
| TC07 | Draft thẳng Approved | Bị từ chối | `CTR03_Transition_DraftToApproved_Rejected`, `CTR03_Submit_Success_GoesToPendingApproval_NotApproved` | `TC07_*` (2 test) | Pass |
| TC08 | Submit lặp | Không tạo thêm xử lý trùng | `TC08_Submit_Twice_SecondRejected_NoDuplicateHistory` | `TC08_SubmitTwice_SecondIs409_*` | Pass |

Không có test nào chạy thủ công. Phần chưa tự động hóa: giao diện Swagger (chỉ kiểm bằng mắt khi demo).

## Đối chiếu quy tắc

| Quy tắc | Code xử lý | Ca đúng | Ca sai |
|---|---|---|---|
| CTR-01 | `ContractService.UpdateAsync`, `AddAttachmentAsync` | Draft / RevisionRequested sửa được | PendingApproval, Approved, Active, Cancelled bị từ chối |
| CTR-02 | `ContractService.SubmitAsync` | Có nội dung hoặc có tệp, khách Active, ngày hợp lệ | Thiếu nội dung và tệp / khách Suspended / StartDate > EndDate |
| CTR-03 | `ContractTransitions.EnsureCanTransition` (gọi trong `SubmitAsync`) | Draft → PendingApproval | Draft → Approved ném `CTR-03` |

## Lỗi đã tìm thấy

### Lỗi 1 — Sửa hợp đồng của khách hàng đã bị Suspended vẫn thành công (đã sửa)
- **Tái hiện:** tạo khách Active, tạo hợp đồng Draft, đổi khách sang Suspended, gọi `PUT /api/contracts/{id}` giữ nguyên `customerId`. Kết quả trả 200 thay vì từ chối.
- **Nguyên nhân:** `UpdateAsync` chỉ kiểm tra khách hàng khi `CustomerId` thay đổi. Đề bài yêu cầu "Customer được tham chiếu phải hợp lệ".
- **Phát hiện bởi:** `ContractRulesTests.Update_ContractOfSuspendedCustomer_Rejected` (fail trước khi sửa).
- **Sửa:** luôn kiểm tra khách hàng tồn tại và Active, kể cả khi không đổi khách ([ContractService.cs](../src/QTKD.Application/Services/ContractService.cs)).

### Lỗi 2 — CTR-03 chỉ được đảm bảo ngầm, không có code chặn (đã sửa)
- **Hiện tượng:** không có đoạn code nào từ chối Draft → Approved, nên không thể viết TC07 đúng nghĩa "bị từ chối".
- **Sửa:** thêm `QTKD.Domain.Rules.ContractTransitions` (bảng chuyển trạng thái) và gọi trong `SubmitAsync`.

### Lỗi 3 — API trả 200 thay vì 201 khi tạo tệp đính kèm / dòng giá (đã sửa)
- **Sửa:** `POST /api/contracts/{id}/attachments` và `POST /api/pricelists/{id}/items` trả 201 Created. Đã cập nhật [api-tuan06.md](api-tuan06.md).

### Lưu ý về test (không phải lỗi ứng dụng)
- `DateTime` của .NET có độ chính xác 100ns, PostgreSQL chỉ lưu đến microsecond, nên khi so `SubmittedAt` phải dùng dung sai 1ms.

## Lỗi / hạn chế còn tồn tại

| # | Mô tả | Mức độ |
|---|---|---|
| 1 | Hai request tạo trùng `ContractNo` / `Code` chạy song song cùng vượt qua bước kiểm tra, request sau chạm unique index và trả 500 thay vì 400. Chưa có test tái hiện vì cần chạy song song. | Trung bình |
| 2 | Các lỗi `DUPLICATE_*` đang trả 400 (nên là 409 Conflict). | Thấp |
| 3 | Chưa có Approval: không có endpoint nào đưa hợp đồng sang Approved/Rejected/RevisionRequested nên các trạng thái này chỉ test được bằng cách dựng dữ liệu trực tiếp. | Theo phạm vi đề bài |
| 4 | Gửi lặp được chặn bằng kiểm tra trạng thái (không dùng khóa lạc quan), hai lần gửi đồng thời có thể cùng đọc Draft. | Thấp |
| 5 | `PerformedBy` do client tự gửi, chưa có đăng nhập / phân quyền. | Theo phạm vi đề bài |

## Cách chạy lại

```powershell
cd "F:\LAB_TH5,6,7\QTKD"
dotnet test                                        # chạy tất cả
dotnet test tests/QTKD.UnitTests                   # chỉ unit test
dotnet test tests/QTKD.IntegrationTests            # cần PostgreSQL đang chạy
```

Integration test **luôn** kết nối database `qtkd_test` (code ép tên database), không bao giờ chạm `qtkd`. Chuỗi kết nối lấy từ biến môi trường `QTKD_TEST_CONNECTION` hoặc từ User Secrets của `QTKD.Api`. Database `qtkd_test` được tự migrate và nạp lại dữ liệu mẫu ở đầu mỗi lần chạy.
