# Technical Design

## Stack

MVP sử dụng:

- .NET 10.
- Target framework `net10.0`.
- ASP.NET Core Web API.
- Controller API.
- SQLite.
- Entity Framework Core.
- Serilog ghi log file.
- HttpClientFactory để gọi Jira REST API.
- Retry bằng `Microsoft.Extensions.Http.Resilience` hoặc Polly-compatible resilience handler.
- Options Pattern để đọc `appsettings`.

## Tích hợp Jira

Không dùng Jira SDK.

Service tự viết `JiraClient` gọi Jira REST API chính thức.

Lý do:

- Scope MVP chỉ cần một số endpoint nhỏ.
- Chủ động kiểm soát logging, retry, sanitize request/response và error mapping.
- Tránh phụ thuộc SDK không còn được maintain tốt.
- Mapping product/project/issue type/status là logic riêng của service.

## Kiến trúc layer

Kiến trúc đề xuất:

```text
Controllers
  -> Application Services
  -> Configuration/Mapping Services
  -> Jira Client
  -> Jira REST API

Infrastructure
  -> SQLite / EF Core
  -> File logging
  -> Retry
```

Trách nhiệm:

- `Controllers`: nhận request, gọi service, trả response chuẩn.
- `Application`: xử lý use case tạo issue, cập nhật status, lấy status.
- `Domain`: enum, constant, model nghiệp vụ đơn giản.
- `Infrastructure/Jira`: gọi Jira REST API.
- `Infrastructure/Persistence`: EF Core DbContext, entity, seed data.
- `Common`: response wrapper, error code, middleware, helper.
- `Options`: class bind cấu hình từ `appsettings`.

## Cấu trúc thư mục đề xuất

```text
src/
  JiraIntegrationService.Api/
    Controllers/
    Application/
      Issues/
      Common/
    Domain/
      Constants/
      Enums/
    Infrastructure/
      Jira/
      Persistence/
      Security/
    Options/
    Common/
      ApiResponse.cs
      ErrorCodes.cs
      TraceId.cs
    Program.cs
    appsettings.json

tests/
  JiraIntegrationService.Tests/
```

## Appsettings đề xuất

```json
{
  "InternalAuth": {
    "Token": "change-me"
  },
  "Jira": {
    "BaseUrl": "https://jira.example.com",
    "ApiBasePath": "/rest/api/2"
  },
  "ConnectionStrings": {
    "Default": "Data Source=jira-integration.db"
  },
  "Retry": {
    "MaxRetryAttempts": 3,
    "DelayMilliseconds": 300
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/jira-integration-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## Database schema MVP

SQLite chỉ lưu cấu hình mapping, chưa lưu lịch sử thao tác.

### Products

Lưu danh sách sản phẩm được phép gọi service.

| Column | Type | Note |
| --- | --- | --- |
| Id | integer | Primary key |
| Code | text | Ví dụ `CRM`, unique |
| Name | text | Tên hiển thị |
| JiraProjectKey | text | Ví dụ `CRM` |
| IsActive | boolean | Bật/tắt product |
| CreatedAt | datetime | Thời điểm tạo |
| UpdatedAt | datetime | Thời điểm cập nhật |

### JiraCredentials

Lưu credential Jira theo product.

| Column | Type | Note |
| --- | --- | --- |
| Id | integer | Primary key |
| ProductId | integer | FK tới `Products` |
| Username | text | Jira username |
| Password | text | Jira password, MVP có thể lưu plain trong SQLite nội bộ |
| IsActive | boolean | Credential đang dùng |
| CreatedAt | datetime | Thời điểm tạo |
| UpdatedAt | datetime | Thời điểm cập nhật |

Ghi chú bảo mật:

- Không log password.
- Sau MVP nên mã hóa password hoặc chuyển sang secret manager.

### IssueTypeMappings

Map issue type nội bộ sang Jira issue type.

| Column | Type | Note |
| --- | --- | --- |
| Id | integer | Primary key |
| ProductId | integer | FK tới `Products` |
| IssueTypeCode | text | Ví dụ `BUG`, `TASK`, `CUSTOMER_REQUEST` |
| JiraIssueTypeName | text | Ví dụ `Bug`, `Task` |
| IsActive | boolean | Bật/tắt mapping |
| CreatedAt | datetime | Thời điểm tạo |
| UpdatedAt | datetime | Thời điểm cập nhật |

Unique đề xuất:

```text
ProductId + IssueTypeCode
```

### FieldMappings

Map field nội bộ sang Jira field/custom field.

| Column | Type | Note |
| --- | --- | --- |
| Id | integer | Primary key |
| ProductId | integer | FK tới `Products` |
| IssueTypeMappingId | integer | Nullable, FK tới `IssueTypeMappings` |
| SourceField | text | Ví dụ `customerId` |
| JiraField | text | Ví dụ `customfield_10010` |
| IsRequired | boolean | Field có bắt buộc không |
| DefaultValue | text | Giá trị mặc định nếu cần |
| IsActive | boolean | Bật/tắt mapping |

Ghi chú:

- Các field cơ bản như `project`, `issuetype`, `summary`, `description` có thể xử lý trực tiếp trong code.
- `FieldMappings` chủ yếu dùng cho custom field.

### StatusMappings

Map status chuẩn nội bộ sang Jira transition và map Jira status về status chuẩn.

| Column | Type | Note |
| --- | --- | --- |
| Id | integer | Primary key |
| ProductId | integer | FK tới `Products` |
| IssueTypeMappingId | integer | Nullable, FK tới `IssueTypeMappings` |
| StandardStatus | text | `OPEN`, `IN_PROGRESS`, `WAITING`, `DONE`, `CANCELLED` |
| JiraStatusName | text | Jira status thật dùng khi đọc status |
| JiraTransitionId | text | Optional, dùng khi update status |
| JiraTransitionName | text | Optional, fallback khi không có transition id |
| IsActive | boolean | Bật/tắt mapping |

Ghi chú:

- Khi update status, ưu tiên match `JiraTransitionId`.
- Nếu không có transition id, match theo `JiraTransitionName`.
- Khi đọc status, map `JiraStatusName` về `StandardStatus`.
- Nếu không map được, trả `UNKNOWN`.

## Jira REST API cần dùng

Giả định Jira Server/Data Center hoặc Jira instance hỗ trợ REST API v2.

Base:

```text
{Jira:BaseUrl}{Jira:ApiBasePath}
```

Ví dụ:

```text
https://jira.example.com/rest/api/2
```

### Create issue

```http
POST /issue
```

Payload Jira mẫu:

```json
{
  "fields": {
    "project": {
      "key": "CRM"
    },
    "issuetype": {
      "name": "Bug"
    },
    "summary": "Customer cannot submit order",
    "description": "Error occurs after clicking submit."
  }
}
```

### Get issue status

```http
GET /issue/{issueIdOrKey}?fields=status
```

### Get available transitions

```http
GET /issue/{issueIdOrKey}/transitions
```

### Transition issue

```http
POST /issue/{issueIdOrKey}/transitions
```

Payload Jira mẫu:

```json
{
  "transition": {
    "id": "31"
  }
}
```

## Jira authentication

MVP dùng Basic Authentication với `username` và `password`.

Credential lấy theo product:

```text
productCode
  -> Product
  -> JiraCredential
  -> Basic Auth khi gọi Jira
```

## TraceId

Mỗi request nên có `traceId`.

Ưu tiên:

1. Dùng `HttpContext.TraceIdentifier`.
2. Nếu client gửi header correlation id sau này thì có thể map tiếp.

`traceId` phải xuất hiện trong:

- API response.
- Application log.
- Jira request/response log rút gọn.

## Logging design

Sử dụng Serilog file rolling theo ngày.

Logging baseline nên được cấu hình sớm trước khi implement Jira client, vì Jira client cần log request/response rút gọn ngay từ đầu. Phase logging cuối chỉ dùng để polish structured fields, sanitize và kiểm tra traceability.

Log nên có structured fields:

- `TraceId`
- `ProductCode`
- `Action`
- `JiraEndpoint`
- `JiraStatusCode`
- `DurationMs`
- `IsSuccess`
- `ErrorCode`

Không log:

- Password.
- Auth token.
- Authorization header.
- Payload quá dài.

## Retry design

Retry nằm trong Jira HTTP client.

Retry cho:

- Timeout.
- HTTP `429`.
- HTTP `500`.
- HTTP `502`.
- HTTP `503`.
- HTTP `504`.

Không retry cho:

- `400`.
- `401`.
- `403`.
- `404`.
- Lỗi mapping/config.

## Issue identifier design

Các API update/get status nhận `jiraIssueId` hoặc `jiraIssueKey`.

Quy tắc tạo `issueIdOrKey` khi gọi Jira:

1. Nếu có `jiraIssueId`, dùng `jiraIssueId`.
2. Nếu không có `jiraIssueId`, dùng `jiraIssueKey`.
3. Không verify consistency khi cả hai cùng được gửi trong MVP.

## Status mapping fallback

Status mapping có thể ở hai cấp:

- Cấp issue type: `ProductId + IssueTypeMappingId + StandardStatus/JiraStatusName`.
- Cấp product: `ProductId + IssueTypeMappingId = null + StandardStatus/JiraStatusName`.

Fallback rule:

1. Nếu request có `issueTypeCode`, tìm mapping cấp issue type trước.
2. Nếu không có hoặc không tìm thấy, tìm mapping cấp product.
3. Khi đọc status, nếu vẫn không map được thì trả `UNKNOWN`.
4. Khi update status, nếu vẫn không map được thì trả `CONFIG_NOT_FOUND`.

## Test strategy MVP

Tối thiểu cần test:

- Validation request.
- Response wrapper.
- Auth middleware/filter.
- Mapping product/issue type/status.
- Jira client parse response thành công.
- Jira client parse response lỗi.
- Retry policy không retry lỗi `400`.
- Get status trả `UNKNOWN` khi không map được.

Nên ưu tiên unit test cho application service và mapping service trước.
