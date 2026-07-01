<<<<<<< HEAD
# jira-integration-service
=======
# Jira Integration Service

## Frontend Admin UI

Frontend nam trong cung repo tai:

```text
src/JiraIntegrationService.Web
```

Chay dev mode:

```powershell
dotnet run --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --launch-profile http
cd src/JiraIntegrationService.Web
npm install
npm run dev
```

Mac dinh Vite chay tai `http://localhost:5173` va proxy `/api` ve `http://localhost:5016`.
Trong UI, vao `Settings` de sua API base URL hoac `X-Internal-Auth` token. Gia tri dev mac dinh la `dev-internal-token`.

Build frontend:

```powershell
cd src/JiraIntegrationService.Web
npm run build
```

Khi frontend da build ra `src/JiraIntegrationService.Web/dist`, ASP.NET API co the serve SPA truc tiep. Khi `dotnet publish`, dist se duoc copy vao `wwwroot` neu `dist/index.html` ton tai:

```powershell
npm --prefix src/JiraIntegrationService.Web run build
dotnet publish src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj -c Release
```

Sau khi chay API/publish output, mo `http://localhost:5016/products` de dung admin UI.

## Configuration Driven Update

Service da duoc nang cap theo huong product moi co the cau hinh qua Admin API ma khong can sua code create issue.

Create issue request moi:

```json
{
  "productCode": "CRM",
  "issueTypeCode": "BUG",
  "data": {
    "summary": "Customer cannot submit order",
    "description": "Error occurs after clicking submit.",
    "priority": "High",
    "customer": {
      "code": "CUST-001"
    },
    "ticket": {
      "url": "https://example.test/tickets/CRM-TICKET-123"
    }
  }
}
```

Admin API chinh:

```text
GET    /api/admin/products
POST   /api/admin/products
GET    /api/admin/products/{code}
PUT    /api/admin/products/{code}
DELETE /api/admin/products/{code}
GET    /api/admin/products/{code}/credential
PUT    /api/admin/products/{code}/credential
GET    /api/admin/products/{code}/issue-types
POST   /api/admin/products/{code}/issue-types
PUT    /api/admin/products/{code}/issue-types/{issueTypeCode}
GET    /api/admin/products/{code}/issue-types/{issueTypeCode}/field-mappings
POST   /api/admin/products/{code}/issue-types/{issueTypeCode}/field-mappings
PUT    /api/admin/field-mappings/{id}
DELETE /api/admin/field-mappings/{id}
GET    /api/admin/products/{code}/issue-types/{issueTypeCode}/status-mappings
POST   /api/admin/products/{code}/issue-types/{issueTypeCode}/status-mappings
PUT    /api/admin/status-mappings/{id}
DELETE /api/admin/status-mappings/{id}
POST   /api/admin/products/{code}/validate-create-issue-config
```

Jira connection khong con chi nam o `Jira:BaseUrl` global. Luong moi lay tu `Products.JiraBaseUrl`, `Products.JiraApiBasePath`, `Products.JiraVersion` va `JiraCredentials`.

ASP.NET Core Web API nội bộ dùng để tạo issue Jira và đồng bộ trạng thái theo bộ status chuẩn của hệ thống.

MVP hiện tại tập trung vào 3 việc chính:

- Tạo issue trên Jira theo `productCode` và `issueTypeCode`.
- Cập nhật trạng thái issue trên Jira bằng status chuẩn nội bộ.
- Lấy trạng thái issue từ Jira và map về status chuẩn nội bộ.

## Trạng Thái Hiện Tại

- Đã hoàn thành phase 1-10: scaffold, API contract, internal auth, SQLite config, mapping service, Jira REST client, create/update/get status API, logging và retry.
- Phase 11 đã verify end-to-end với fake Jira REST server local.
- Chưa verify với Jira test instance thật vì repo chưa có Jira URL và credential thật.
- Phase 12 là tài liệu bàn giao này và các ghi chú liên quan.

## Công Nghệ

- .NET 10, ASP.NET Core Web API.
- EF Core SQLite cho config database.
- Jira REST API tự viết bằng `HttpClient`, không dùng Jira SDK.
- Serilog ghi console và file rolling theo ngày.
- xUnit cho test.

## Cấu Trúc Chính

- `src/JiraIntegrationService.Api`: Web API.
- `tests/JiraIntegrationService.Tests`: test project.
- `docs`: tài liệu thiết kế, spec, plan và handoff.
- `dotnet-tools.json`: local tool manifest, hiện có `dotnet-ef` version `10.0.9`.

## Chạy Local

Yêu cầu máy có .NET 10 SDK.

```powershell
dotnet --list-sdks
dotnet restore
dotnet tool restore
dotnet build
dotnet test
```

Chạy API bằng launch profile `http`:

```powershell
dotnet run --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --launch-profile http
```

URL mặc định:

- HTTP: `http://localhost:5016`
- Health check: `GET http://localhost:5016/health`
- OpenAPI chỉ map trong `Development`: `http://localhost:5016/openapi/v1.json`

## Cấu Hình Appsettings

File chính:

- `src/JiraIntegrationService.Api/appsettings.json`
- `src/JiraIntegrationService.Api/appsettings.Development.json`

Các cấu hình quan trọng:

| Key | Ý nghĩa | Giá trị mẫu |
| --- | --- | --- |
| `InternalAuth:Token` | Token nội bộ dùng ở header `X-Internal-Auth` | `dev-internal-token` |
| `ConnectionStrings:Default` | SQLite connection string | `Data Source=jira-integration.db` |
| `Jira:BaseUrl` | Jira URL dùng chung cho MVP | `https://jira.example.com` |
| `Jira:ApiBasePath` | REST API base path của Jira | `/rest/api/2` |
| `Retry:MaxAttempts` | Số lần thử tối đa cho lỗi tạm thời | `3` |
| `Retry:DelayMilliseconds` | Delay giữa các lần retry | `100` hoặc `300` |
| `Serilog:WriteTo` | Console/file logging | `logs/jira-integration-.log` |

Ví dụ override bằng environment variable trong PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5016"
$env:InternalAuth__Token = "dev-internal-token"
$env:Jira__BaseUrl = "https://jira.company.local"
$env:Jira__ApiBasePath = "/rest/api/2"
dotnet run --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --no-launch-profile
```

Lưu ý: token nội bộ nằm trong `appsettings`, còn Jira username/password theo product đang lưu ở SQLite bảng `JiraCredentials`.

## Migration Và Database

Trong môi trường `Development`, service tự chạy EF migration khi start.

Muốn chạy migration chủ động:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --startup-project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj
```

Với cấu hình mặc định, database local nằm tại:

```text
src/JiraIntegrationService.Api/jira-integration.db
```

## Seed Và Update Config Thủ Công

Dữ liệu mặc định **không còn** seed qua EF Core `HasData`. Migration giờ chỉ tạo schema;
toàn bộ dữ liệu mặc định nằm trong file SQL `scripts/insert-product-config.template.sql`.
Nhờ vậy bạn có thể sửa giá trị mặc định trực tiếp trong file SQL mà **không cần tạo migration mới**.

Dữ liệu mặc định (product `EAS`) do script nạp:

- Product `EAS`, Jira project key `EAS`, base url `https://jira.ezcloudhotel.com`.
- Jira credential: auth `Basic`, username `anh.phamviet`.
- Issue type: `BUG` -> `Bug`, `TASK` -> `Task`.
- Template `DEFAULT` cho mỗi issue type.
- Field mapping cho `BUG`: `data.summary`, `data.description`, `data.priority`, `data.customer.code`, `data.ticket.url`.
- Status chuẩn cho `BUG`: `OPEN`, `IN_PROGRESS`, `WAITING`, `DONE`, `CANCELLED`.
- Khi đọc trạng thái Jira mà không map được, service trả `UNKNOWN`.

Luồng tạo lại DB từ đầu:

1. Xóa file `src/JiraIntegrationService.Api/jira-integration.db`.
2. `dotnet run` (migration tạo schema rỗng, không có dữ liệu).
3. Chạy script SQL để nạp cấu hình mặc định.

Script là idempotent (dùng UPSERT), chạy lại nhiều lần vẫn an toàn. Muốn đổi giá trị mặc định
thì sửa trực tiếp trong file rồi chạy lại:

```powershell
sqlite3 src/JiraIntegrationService.Api/jira-integration.db ".read scripts/insert-product-config.template.sql"
```

Muốn chỉnh nhanh từng bản ghi cũng có thể dùng DB Browser for SQLite, `sqlite3`, hoặc tool tương đương.

Thứ tự cấu hình cho sản phẩm mới:

1. Thêm `Products`.
2. Thêm `JiraCredentials` cho product.
3. Thêm `IssueTypeMappings`.
4. Thêm `IssueFieldMappings` nếu cần custom fields.
5. Thêm `StatusMappings`.

Ví dụ update credential Jira cho `EAS`:

```sql
UPDATE JiraCredentials
SET Username = 'jira-user',
    PasswordOrToken = 'jira-password-or-token',
    UpdatedAt = datetime('now')
WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
  AND IsActive = 1;
```

Ví dụ thêm product mới:

```sql
INSERT INTO Products (Code, Name, JiraProjectKey, JiraBaseUrl, JiraApiBasePath, JiraVersion, IsActive, CreatedAt, UpdatedAt)
VALUES ('OPS', 'Operations', 'OPS', 'https://jira.ezcloudhotel.com', '/rest/api/2', 'ServerV2', 1, datetime('now'), datetime('now'));

INSERT INTO JiraCredentials (ProductId, AuthType, Username, PasswordOrToken, IsActive, CreatedAt, UpdatedAt)
SELECT Id, 'Basic', 'jira-ops-user', 'change-me', 1, datetime('now'), datetime('now')
FROM Products
WHERE Code = 'OPS';
```

Ví dụ thêm issue type riêng cho product:

```sql
INSERT INTO IssueTypeMappings (ProductId, IssueTypeCode, JiraIssueTypeName, IsActive, CreatedAt, UpdatedAt)
SELECT Id, 'INCIDENT', 'Incident', 1, datetime('now'), datetime('now')
FROM Products
WHERE Code = 'OPS';
```

Ví dụ thêm field mapping:

```sql
INSERT INTO IssueFieldMappings (ProductId, IssueTypeMappingId, TemplateCode, SourcePath, JiraField, ValueType, ValueShape, IsRequired, DefaultValue, SortOrder, IsActive, CreatedAt, UpdatedAt)
SELECT p.Id, it.Id, 'DEFAULT', 'data.ticket.url', 'customfield_10011', 'string', 'raw', 0, NULL, 10, 1, datetime('now'), datetime('now')
FROM Products p
JOIN IssueTypeMappings it ON it.ProductId = p.Id
WHERE p.Code = 'OPS'
  AND it.IssueTypeCode = 'INCIDENT';
```

Ví dụ thêm status mapping cho một issue type:

```sql
INSERT INTO StatusMappings (
    ProductId,
    IssueTypeMappingId,
    StandardStatus,
    JiraStatusName,
    JiraTransitionId,
    JiraTransitionName,
    IsActive
)
SELECT p.Id, it.Id, 'IN_PROGRESS', 'In Progress', '31', 'Start Progress', 1
FROM Products p
JOIN IssueTypeMappings it ON it.ProductId = p.Id
WHERE p.Code = 'OPS'
  AND it.IssueTypeCode = 'INCIDENT';
```

Muốn tạo fallback cấp product, set `IssueTypeMappingId` là `NULL` trong `StatusMappings`.

## Auth Nội Bộ

Các API nghiệp vụ cần header:

```text
X-Internal-Auth: dev-internal-token
```

`GET /health` không cần auth.

Ví dụ PowerShell:

```powershell
$headers = @{ "X-Internal-Auth" = "dev-internal-token" }
Invoke-RestMethod -Method Get -Uri "http://localhost:5016/health"
```

## Response Chuẩn

Thành công:

```json
{
  "success": true,
  "data": {},
  "traceId": "trace-id"
}
```

Lỗi:

```json
{
  "success": false,
  "errorCode": "VALIDATION_ERROR",
  "message": "Invalid request.",
  "traceId": "trace-id"
}
```

Error code chính:

- `VALIDATION_ERROR`
- `AUTH_ERROR`
- `CONFIG_NOT_FOUND`
- `JIRA_ERROR`
- `INTERNAL_ERROR`

## Endpoint MVP

Postman collection có sẵn tại:

```text
postman/JiraIntegrationService.postman_collection.json
```

Import file này vào Postman, chỉnh collection variables nếu cần, rồi chạy theo thứ tự `GET /health` -> `POST /api/issues/create` -> `POST /api/issues/status/update` -> `GET /api/issues/status`.

### GET /health

Không cần auth.

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5016/health"
```

### POST /api/issues/create

Tạo issue Jira và trả về `jiraIssueId`, `jiraIssueKey`.

```powershell
$headers = @{ "X-Internal-Auth" = "dev-internal-token" }
$body = @{
  productCode = "CRM"
  issueTypeCode = "BUG"
  summary = "Customer cannot submit order"
  description = "Error occurs after clicking submit."
  priority = "High"
  reporter = "user@example.com"
  assignee = "developer@example.com"
  customFields = @{
    customerId = "CUST-001"
    sourceRecordId = "CRM-TICKET-123"
  }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5016/api/issues/create" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $body
```

Response data:

```json
{
  "jiraIssueId": "10001",
  "jiraIssueKey": "CRM-123"
}
```

### POST /api/issues/status/update

Cập nhật trạng thái Jira bằng status chuẩn nội bộ.

```powershell
$headers = @{ "X-Internal-Auth" = "dev-internal-token" }
$body = @{
  productCode = "CRM"
  jiraIssueKey = "CRM-123"
  issueTypeCode = "BUG"
  standardStatus = "IN_PROGRESS"
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5016/api/issues/status/update" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $body
```

Ghi chú:

- Cần có `jiraIssueId` hoặc `jiraIssueKey`.
- Nếu gửi cả hai, service dùng `jiraIssueId` khi gọi Jira.
- Không cho update sang `UNKNOWN`.

### GET /api/issues/status

Lấy trạng thái Jira và map về status chuẩn nội bộ.

```powershell
$headers = @{ "X-Internal-Auth" = "dev-internal-token" }
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5016/api/issues/status?productCode=CRM&jiraIssueKey=CRM-123&issueTypeCode=BUG" `
  -Headers $headers
```

Response data:

```json
{
  "standardStatus": "IN_PROGRESS"
}
```

Nếu Jira status không có mapping, response data trả:

```json
{
  "standardStatus": "UNKNOWN"
}
```

## Logging

Log runtime mặc định nằm trong:

```text
src/JiraIntegrationService.Api/logs/
```

Mỗi request có `traceId`. Khi API trả lỗi, dùng `traceId` trong response để tìm log liên quan.

Jira request/response log đã được sanitize và truncate. Không log `Authorization`, `X-Internal-Auth`, password, token hoặc secret field.

## Phần Chưa Làm

- Chưa verify với Jira test instance thật.
- Chưa có attachment/file đính kèm.
- Chưa có audit/history database cho nghiệp vụ.
- Chưa có config API hoặc admin UI.
- Chưa mã hóa credential trong SQLite.
- Chưa có auth nội bộ riêng theo product.
- Chưa hỗ trợ nhiều Jira base URL.
- Chưa có Jira webhook.
- Chưa có queue/message broker.
- Chưa có Docker.

## Tài Liệu Nên Đọc Tiếp

Đọc theo thứ tự trong `docs/README.md`. File quan trọng nhất khi tiếp tục code là `docs/04-implementation-plan.md` vì có checkbox phase và backlog.
>>>>>>> eed623ad4a329b82166f085eab16290b9afeca21
