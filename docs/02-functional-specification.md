# Functional Specification

## Configuration Driven Contract Update

`POST /api/issues/create` hien tai dung request:

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

Service build Jira payload bang `IssueFieldMappings`, khong hardcode field nghiep vu theo product. Admin API nam duoi `/api/admin/...` de cau hinh product, credential, issue type, field mappings, status mappings va validate create issue config.

`POST /api/issues/status/update` sau khi goi Jira transition se goi lai Jira issue status va verify voi `StatusMappings.JiraStatusName`.

## API convention

Service là HTTP REST API nội bộ.

MVP chỉ dùng:

- `GET`
- `POST`

Không dùng trong MVP:

- `PUT`
- `PATCH`
- `DELETE`

## Issue identifier convention

Các API thao tác trên Jira issue hỗ trợ cả:

- `jiraIssueId`
- `jiraIssueKey`

Quy tắc MVP:

- Client phải gửi ít nhất một trong hai giá trị.
- Nếu gửi cả hai, service dùng `jiraIssueId` làm định danh chính khi gọi Jira vì Jira issue id ổn định hơn issue key.
- Service không gọi thêm Jira chỉ để kiểm tra `jiraIssueId` và `jiraIssueKey` có trỏ tới cùng một issue hay không.
- Response có thể echo lại identifier client đã gửi; field còn lại có thể `null` nếu Jira API không trả thêm thông tin.

## Internal authentication

MVP dùng một token cố định trong `appsettings`.

Client gửi token qua header:

```http
X-Internal-Auth: <token>
```

Nếu thiếu hoặc sai token, service trả lỗi:

```json
{
  "success": false,
  "errorCode": "AUTH_ERROR",
  "message": "Unauthorized request.",
  "traceId": "..."
}
```

## Response chuẩn

Tất cả API trả body theo format thống nhất.

Response thành công:

```json
{
  "success": true,
  "data": {},
  "traceId": "..."
}
```

Response lỗi:

```json
{
  "success": false,
  "errorCode": "VALIDATION_ERROR",
  "message": "Invalid request.",
  "traceId": "..."
}
```

## Error code MVP

Bộ error code ban đầu giữ đơn giản:

- `VALIDATION_ERROR`
- `AUTH_ERROR`
- `CONFIG_NOT_FOUND`
- `JIRA_ERROR`
- `INTERNAL_ERROR`

## API: Health check

```http
GET /health
```

Mục tiêu:

- Kiểm tra service còn chạy.
- Có thể dùng cho monitoring nội bộ đơn giản.

Response mẫu:

```json
{
  "success": true,
  "data": {
    "status": "OK"
  },
  "traceId": "..."
}
```

## API: Tạo Jira issue

```http
POST /api/issues/create
```

### Request body

```json
{
  "productCode": "CRM",
  "issueTypeCode": "BUG",
  "summary": "Customer cannot submit order",
  "description": "Error occurs after clicking submit.",
  "priority": "High",
  "reporter": "user@example.com",
  "assignee": "developer@example.com",
  "customFields": {
    "customerId": "CUST-001",
    "sourceRecordId": "CRM-TICKET-123"
  }
}
```

### Field bắt buộc

- `productCode`
- `issueTypeCode`
- `summary`

### Field tùy chọn

- `description`
- `priority`
- `reporter`
- `assignee`
- `customFields`

### Luồng xử lý

1. Validate request.
2. Kiểm tra internal auth.
3. Tìm product config theo `productCode`.
4. Tìm Jira project key của product.
5. Tìm issue type mapping theo `productCode` và `issueTypeCode`.
6. Map field nội bộ sang Jira field.
7. Gọi Jira REST API tạo issue.
8. Log request/response rút gọn.
9. Trả `jiraIssueId` và `jiraIssueKey`.

### Response thành công

```json
{
  "success": true,
  "data": {
    "jiraIssueId": "10001",
    "jiraIssueKey": "CRM-123"
  },
  "traceId": "..."
}
```

### Lỗi thường gặp

- Product không tồn tại: `CONFIG_NOT_FOUND`.
- Issue type không được cấu hình: `CONFIG_NOT_FOUND`.
- Jira trả lỗi validation: `JIRA_ERROR`.
- Jira credential sai: `JIRA_ERROR`.

## API: Cập nhật trạng thái Jira issue

```http
POST /api/issues/status/update
```

### Request body

```json
{
  "productCode": "CRM",
  "jiraIssueKey": "CRM-123",
  "jiraIssueId": null,
  "issueTypeCode": "BUG",
  "standardStatus": "IN_PROGRESS"
}
```

### Field bắt buộc

- `productCode`
- `standardStatus`
- Ít nhất một trong hai field:
  - `jiraIssueKey`
  - `jiraIssueId`

### Field khuyến nghị

- `issueTypeCode`

`issueTypeCode` giúp service chọn mapping chính xác hơn nếu Jira workflow khác nhau theo issue type.

Nếu có `issueTypeCode`, service ưu tiên mapping theo `productCode + issueTypeCode + standardStatus`.

Nếu không có mapping theo issue type hoặc request không gửi `issueTypeCode`, service fallback sang mapping cấp product: `productCode + standardStatus`.

### Trạng thái hợp lệ để update

- `OPEN`
- `IN_PROGRESS`
- `WAITING`
- `DONE`
- `CANCELLED`

Không cho phép update sang:

- `UNKNOWN`

### Luồng xử lý

1. Validate request.
2. Kiểm tra internal auth.
3. Tìm product config.
4. Tìm status mapping từ `standardStatus` sang Jira transition.
5. Gọi Jira API lấy danh sách transition khả dụng của issue.
6. Chọn transition phù hợp theo mapping.
7. Gọi Jira API thực hiện transition.
8. Log request/response rút gọn.
9. Trả kết quả thành công.

### Response thành công

```json
{
  "success": true,
  "data": {
    "jiraIssueId": "10001",
    "jiraIssueKey": "CRM-123",
    "standardStatus": "IN_PROGRESS"
  },
  "traceId": "..."
}
```

### Lỗi thường gặp

- Không tìm thấy product config: `CONFIG_NOT_FOUND`.
- Không tìm thấy status mapping: `CONFIG_NOT_FOUND`.
- Transition không khả dụng trên Jira: `JIRA_ERROR`.
- Jira issue không tồn tại: `JIRA_ERROR`.

## API: Lấy trạng thái Jira issue

```http
GET /api/issues/status?productCode=CRM&jiraIssueKey=CRM-123
```

Hoặc:

```http
GET /api/issues/status?productCode=CRM&jiraIssueId=10001
```

### Query bắt buộc

- `productCode`
- Ít nhất một trong hai:
  - `jiraIssueKey`
  - `jiraIssueId`

### Query tùy chọn

- `issueTypeCode`

Nếu có `issueTypeCode`, service ưu tiên map Jira status theo issue type. Nếu không có, service map theo product-level status mapping.

### Luồng xử lý

1. Validate query.
2. Kiểm tra internal auth.
3. Tìm product config.
4. Gọi Jira API lấy issue field `status`.
5. Map Jira status thật sang status chuẩn nội bộ.
6. Nếu không map được, trả `UNKNOWN`.
7. Log request/response rút gọn.

### Response thành công

```json
{
  "success": true,
  "data": {
    "standardStatus": "IN_PROGRESS"
  },
  "traceId": "..."
}
```

Nếu không map được:

```json
{
  "success": true,
  "data": {
    "standardStatus": "UNKNOWN"
  },
  "traceId": "..."
}
```

## Mapping rule

Service chịu trách nhiệm mapping:

```text
productCode
  -> Jira project key
  -> Jira credential

productCode + issueTypeCode
  -> Jira issue type

productCode + issueTypeCode + field
  -> Jira field/custom field

productCode + issueTypeCode + standardStatus
  -> Jira transition

productCode + issueTypeCode + jiraStatusName
  -> standardStatus
```

Fallback rule:

1. Nếu có `issueTypeCode`, ưu tiên mapping theo issue type.
2. Nếu không có mapping theo issue type, dùng mapping cấp product.
3. Nếu đọc status mà vẫn không map được, trả `UNKNOWN`.
4. Nếu update status mà không có mapping hợp lệ, trả `CONFIG_NOT_FOUND`.

## Logging

MVP log request/response Jira rút gọn vào file.

Nên log:

- `traceId`
- `productCode`
- action: `CreateIssue`, `UpdateStatus`, `GetStatus`
- Jira endpoint
- Jira HTTP method
- request payload rút gọn
- response payload rút gọn
- HTTP status code
- duration
- error message rút gọn nếu có

Không log:

- Jira password
- Internal auth token
- Authorization header
- Nội dung description quá dài
- Dữ liệu nhạy cảm đầy đủ

## Retry

Retry 2-3 lần cho lỗi tạm thời:

- Timeout/network error.
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
- Lỗi validation.
- Lỗi config mapping.
- Transition không hợp lệ.
