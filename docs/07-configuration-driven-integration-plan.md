# Configuration Driven Integration Plan

File này là kế hoạch nâng cấp Jira Integration Service để khi tích hợp product mới, người dùng chỉ cần cấu hình DB qua Admin API, không cần sửa code tạo issue.

## Quyết Định Đã Chốt

- Request tạo issue mới dùng format `productCode + issueTypeCode + data`.
- Product mới được cấu hình qua Admin API, không chỉ insert SQL thủ công.
- Phase đầu target Jira Server/Data Center REST API v2.
- Thiết kế có chỗ mở để sau này hỗ trợ Jira Cloud hoặc version khác.
- Mapping field phase đầu đi mức A:
  - `sourcePath -> jiraField`
  - `valueType`: `string`, `number`, `boolean`, `date`, `object`, `array`
  - `valueShape`: `raw`, `name`, `id`, `value`, `arrayOfName`, `arrayOfId`
  - `isRequired`
  - `defaultValue`
- Phase đầu chưa làm transform mức B như concat, template, enum map, condition, date format.
- Được phép rework DB schema và xóa dữ liệu cũ.

## Mục Tiêu

Luồng tạo issue phải chạy dựa trên cấu hình, ví dụ request:

```json
{
  "productCode": "CRM",
  "issueTypeCode": "BUG",
  "data": {
    "summary": "Không đăng nhập được",
    "description": "User báo lỗi khi login",
    "priority": "High",
    "customer": {
      "code": "C001",
      "name": "ABC Corp"
    },
    "ticket": {
      "id": "TCK-123",
      "url": "https://example.test/tickets/TCK-123"
    }
  }
}
```

Service không hardcode field nghiệp vụ của product. Service đọc mapping trong DB để build Jira payload:

```text
data.summary       -> summary
data.description   -> description
data.priority      -> priority, shape name
data.customer.code -> customfield_10010
data.ticket.url    -> customfield_10011
```

Payload gửi Jira:

```json
{
  "fields": {
    "project": { "key": "CRM" },
    "issuetype": { "name": "Bug" },
    "summary": "Không đăng nhập được",
    "description": "User báo lỗi khi login",
    "priority": { "name": "High" },
    "customfield_10010": "C001",
    "customfield_10011": "https://example.test/tickets/TCK-123"
  }
}
```

## DB Schema Mục Tiêu

### Products

```text
Id
Code
Name
JiraProjectKey
JiraBaseUrl
JiraApiBasePath
JiraVersion
IsActive
CreatedAt
UpdatedAt
```

Gợi ý giá trị:

```text
JiraApiBasePath = /rest/api/2
JiraVersion = ServerV2
```

### JiraCredentials

```text
Id
ProductId
AuthType
Username
PasswordOrToken
IsActive
CreatedAt
UpdatedAt
```

Phase đầu chỉ cần `AuthType = Basic`.

### IssueTypeMappings

```text
Id
ProductId
IssueTypeCode
JiraIssueTypeId nullable
JiraIssueTypeName nullable
IsActive
CreatedAt
UpdatedAt
```

Khi build payload, ưu tiên `JiraIssueTypeId` nếu có, fallback `JiraIssueTypeName`.

### IssueFieldMappings

Thay hoặc nâng cấp bảng `FieldMappings` hiện tại.

```text
Id
ProductId
IssueTypeMappingId nullable
SourcePath
JiraField
ValueType
ValueShape
IsRequired
DefaultValue
SortOrder
IsActive
TransformConfigJson nullable
CreatedAt
UpdatedAt
```

`TransformConfigJson` để dành cho phase sau, chưa xử lý trong phase đầu.

Ví dụ:

```text
SourcePath = data.priority
JiraField = priority
ValueType = string
ValueShape = name
```

Build ra:

```json
{
  "priority": { "name": "High" }
}
```

### StatusMappings

Giữ gần với schema hiện tại:

```text
Id
ProductId
IssueTypeMappingId nullable
StandardStatus
JiraStatusName
JiraTransitionId nullable
JiraTransitionName nullable
IsActive
```

## Admin API Cần Thêm

### Product

```http
GET    /api/admin/products
POST   /api/admin/products
GET    /api/admin/products/{code}
PUT    /api/admin/products/{code}
DELETE /api/admin/products/{code}
```

### Credential

```http
GET /api/admin/products/{code}/credential
PUT /api/admin/products/{code}/credential
```

### Issue Type Mapping

```http
GET  /api/admin/products/{code}/issue-types
POST /api/admin/products/{code}/issue-types
PUT  /api/admin/products/{code}/issue-types/{issueTypeCode}
```

### Field Mapping

```http
GET    /api/admin/products/{code}/issue-types/{issueTypeCode}/field-mappings
POST   /api/admin/products/{code}/issue-types/{issueTypeCode}/field-mappings
PUT    /api/admin/field-mappings/{id}
DELETE /api/admin/field-mappings/{id}
```

### Status Mapping

```http
GET  /api/admin/products/{code}/issue-types/{issueTypeCode}/status-mappings
POST /api/admin/products/{code}/issue-types/{issueTypeCode}/status-mappings
PUT  /api/admin/status-mappings/{id}
```

### Config Validation

```http
POST /api/admin/products/{code}/validate-create-issue-config
```

Phase đầu validate nội bộ:

- Product active.
- Có Jira base URL, project key, api base path.
- Có credential active.
- Có issue type mapping active.
- Có mapping cho `summary`.
- Không duplicate `JiraField` trong cùng product và issue type, trừ khi có rule rõ ràng.
- Required mapping có `SourcePath` hoặc `DefaultValue`.

Phase sau có thể validate thêm bằng Jira metadata API.

## Mapping Engine

Thêm các service:

```text
ISourcePathResolver
IJiraIssuePayloadBuilder
IJiraFieldValueBuilder
```

Luồng create issue mới:

```text
IssueService.CreateIssueAsync
-> load ProductConfig
-> load IssueTypeMapping
-> load IssueFieldMappings
-> build Jira payload từ request.data
-> JiraClient.CreateIssueAsync
```

Payload builder xử lý:

- Đọc value từ `SourcePath`.
- Dùng `DefaultValue` nếu source value thiếu hoặc rỗng.
- Check `IsRequired`.
- Convert theo `ValueType`.
- Wrap theo `ValueShape`.
- Gán vào `fields[JiraField]`.

### ValueShape Phase Đầu

```text
raw         -> "customfield_10010": "ABC"
name        -> "priority": { "name": "High" }
id          -> "customfield_10020": { "id": "10123" }
value       -> "customfield_10021": { "value": "Option A" }
arrayOfName -> "components": [{ "name": "Core" }]
arrayOfId   -> "components": [{ "id": "10001" }]
```

## Jira Client

Hiện tại `Jira:BaseUrl` là global config. Cần chuyển sang connection theo product.

Thiết kế đề xuất:

```text
IJiraClient
JiraServerV2Client
IJiraClientResolver hoặc IJiraClientFactory
```

Phase đầu chỉ implement `JiraServerV2Client`, nhưng `IssueService` không phụ thuộc trực tiếp vào version cụ thể.

Jira connection lấy từ product config:

```text
Product.JiraBaseUrl
Product.JiraApiBasePath
Product.JiraVersion
JiraCredential.AuthType
JiraCredential.Username
JiraCredential.PasswordOrToken
```

## Create Issue Model Mới

```csharp
public sealed class CreateIssueRequest
{
    public string? ProductCode { get; init; }

    public string? IssueTypeCode { get; init; }

    public JsonElement Data { get; init; }
}
```

Vì dự án cho phép rework thoải mái, phase này không cần giữ backward compatibility với request cũ.

## Status Update

Giữ flow transition hiện tại, nhưng:

- Lấy Jira connection theo product, không dùng global `Jira:BaseUrl`.
- Sau khi POST transition thành công, nên gọi lại Jira status.
- Verify status thực tế bằng `JiraStatusName` hoặc map ngược về `StandardStatus`.

Luồng mong muốn:

```text
GET available transitions
-> chọn transition theo JiraTransitionId hoặc JiraTransitionName
-> POST transition
-> GET issue status
-> so sánh với JiraStatusName hoặc standardStatus mapping
-> trả response
```

Việc này giúp phát hiện config sai, ví dụ mapping `IN_PROGRESS` nhưng transition thực tế chuyển issue sang `Done`.

## Phase Code

### Phase 1: Rework DB

- [x] Update entities.
- [x] Update `AppDbContext`.
- [x] Xóa migration cũ nếu cần.
- [x] Tạo migration mới.
- [x] Seed product mẫu CRM.
- [x] Seed issue type, field mappings, status mappings mẫu.
- [x] Update docs database schema.

Ghi chú: file SQLite local `src/JiraIntegrationService.Api/jira-integration.db` đang bị process `dotnet` giữ lock khi thực hiện Phase 1, nên chưa xóa/apply lại DB local trong lượt này.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx` pass: 54 tests.

### Phase 2: Admin API

- [x] Tạo request/response models cho Admin API.
- [x] Tạo admin service hoặc config management service.
- [x] CRUD product.
- [x] CRUD credential.
- [x] CRUD issue type mapping.
- [x] CRUD field mapping.
- [x] CRUD status mapping.
- [x] Thêm endpoint validate create issue config.
- [x] Thêm validation lỗi rõ ràng.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx` pass: 57 tests.

### Phase 3: Mapping Engine

- [x] Implement `ISourcePathResolver`.
- [x] Implement `IJiraFieldValueBuilder`.
- [x] Implement `IJiraIssuePayloadBuilder`.
- [x] Hỗ trợ `ValueType`.
- [x] Hỗ trợ `ValueShape`.
- [x] Hỗ trợ required/default.
- [x] Unit test mapping engine.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx` pass: 67 tests.

### Phase 4: Jira Client Theo Product

- [x] Tạo Jira connection config model.
- [x] Đổi client để nhận connection theo product.
- [x] Tạo `JiraServerV2Client`.
- [x] Tạo resolver/factory theo `JiraVersion`.
- [x] Xóa hoặc giảm phụ thuộc vào global `Jira:BaseUrl`.

Ghi chú: implementation hiện vẫn dùng class `JiraClient`, nhưng được resolve như ServerV2 client qua `IJiraClientResolver`.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx` pass: 67 tests.

### Phase 5: Update Create Issue Flow

- [x] Đổi `CreateIssueRequest` sang `productCode + issueTypeCode + data`.
- [x] Update `IssueService.CreateIssueAsync`.
- [x] Build payload từ DB mapping.
- [x] Gửi payload qua Jira Server v2 client.
- [x] Update controller tests.
- [x] Update service tests.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx` pass: 65 tests.

### Phase 6: Update Status Flow

- [x] Lấy Jira connection theo product.
- [x] Giữ selection transition hiện tại.
- [x] Gọi lại Jira status sau transition.
- [x] Verify status thực tế.
- [x] Test case config sai transition.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx` pass: 67 tests.

### Phase 7: Tests, Docs, Postman

- [x] Unit tests cho source path resolver.
- [x] Unit tests cho value builder.
- [x] Unit tests cho create issue dynamic mapping.
- [x] Integration test thêm product mới qua Admin API rồi create issue.
- [x] Update Postman collection.
- [x] Update README.
- [x] Update handoff notes.

Verification:

- [x] Postman collection JSON parse check pass.
- [x] `dotnet test JiraIntegrationService.slnx` pass: 67 tests.

## Test Cases Bắt Buộc

- [x] `data.customer.code` resolve đúng nested value.
- [x] Required field thiếu thì trả validation error.
- [x] Default value hoạt động khi source value thiếu.
- [x] `raw` build đúng primitive value.
- [x] `name` build `{ "name": value }`.
- [x] `id` build `{ "id": value }`.
- [x] `value` build `{ "value": value }`.
- [x] `arrayOfName` build array object name.
- [x] `arrayOfId` build array object id.
- [x] Product mới tạo qua Admin API có thể create issue mà không sửa code.
- [x] Status update phát hiện transition config sai nếu Jira status sau update không khớp.

## Ghi Chú Cho Phase Sau

Mapping mức B có thể mở rộng qua `TransformConfigJson`:

```json
{
  "type": "template",
  "template": "[{data.system}] {data.title}"
}
```

Các transform có thể thêm sau:

- `concat`
- `template`
- `enumMap`
- `dateFormat`
- `condition`
- `fallbackChain`

Thiết kế phase đầu không implement các transform này, nhưng không khóa đường nâng cấp.
