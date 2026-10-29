# Tổng quan solution QTKD

## Các thành phần và chiều phụ thuộc

```mermaid
flowchart LR
  Client[Swagger / Postman] --> Api[QTKD.Api<br/>Controllers]
  Api --> App[QTKD.Application<br/>Services, DTOs]
  App --> Dom[QTKD.Domain<br/>Entities, Enums]
  Infra[QTKD.Infrastructure<br/>DbContext, Migrations, Seed] --> App
  Api -. đăng ký DI .-> Infra
  Infra --> PG[(PostgreSQL)]
```

| Project | Vai trò |
|---|---|
| QTKD.Domain | Entity, enum, exception nghiệp vụ; không phụ thuộc project nào |
| QTKD.Application | Interface, DTO, service xử lý nghiệp vụ (Tuần 06 đặt CTR-01..03 tại đây) |
| QTKD.Infrastructure | EF Core, DbContext, migration, seed dữ liệu |
| QTKD.Api | Controller HTTP, cấu hình, Swagger |
| tests/QTKD.UnitTests | Unit test nghiệp vụ (Tuần 07) |
| tests/QTKD.IntegrationTests | Test API + database kiểm thử riêng (Tuần 07) |

## Quy trình một request: `GET /api/contracts/1`

```mermaid
sequenceDiagram
  participant C as Client
  participant A as ContractsController
  participant S as ContractService
  participant D as QtkdDbContext
  participant P as PostgreSQL
  C->>A: GET /api/contracts/1
  A->>S: GetByIdAsync(1)
  S->>D: Contracts.Where(Id == 1) + Customer + Attachments
  D->>P: SELECT ... (SQL do EF Core sinh)
  P-->>D: dòng dữ liệu
  D-->>S: ContractDto
  S-->>A: ContractDto hoặc null
  A-->>C: 200 + JSON, hoặc 404 nếu null
```

## Quan hệ giữa các bảng

```mermaid
erDiagram
  customers ||--o{ contracts : "có"
  contracts ||--o{ contract_attachments : "đính kèm"
  contracts ||--o{ contract_histories : "ghi lịch sử"
  price_lists ||--o{ price_items : "gồm"

  customers {
    int Id PK
    string Code UK
    string Name
    string TaxCode
    string Address
    string Status
  }
  contracts {
    int Id PK
    string ContractNo UK
    int CustomerId FK
    date StartDate
    date EndDate
    string ServiceGroup
    text Content
    string Status
    datetime SubmittedAt
  }
  contract_attachments {
    int Id PK
    int ContractId FK
    string FileName
    datetime UploadedAt
  }
  contract_histories {
    int Id PK
    int ContractId FK
    string Action
    string FromStatus
    string ToStatus
    string PerformedBy
    datetime PerformedAt
    string Note
  }
  price_lists {
    int Id PK
    string Name
    date EffectiveDate
  }
  price_items {
    int PriceListId PK
    string ServiceCode PK
    decimal UnitPrice
  }
```
