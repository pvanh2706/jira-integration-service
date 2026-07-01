# Hướng Dẫn Admin Cấu Hình

Tài liệu này hướng dẫn admin cấu hình product, credential, issue type, field mapping và status mapping để tạo/cập nhật issue Jira mà không cần sửa code.

Phạm vi hiện tại:

- Hỗ trợ Jira Server/Data Center REST API v2.
- `jiraVersion` nên dùng `ServerV2`.
- `jiraApiBasePath` nên dùng `/rest/api/2`.
- Chưa có login riêng cho admin UI. API nội bộ dùng header `X-Internal-Auth`.
- Mapping nâng cao bằng `TransformConfigJson` mới được lưu lại, chưa được backend xử lý trong phase hiện tại.

## 1. Truy Cập Admin UI

Nếu chạy backend trực tiếp:

```powershell
dotnet run --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --launch-profile http
```

Backend mặc định chạy tại:

```text
http://localhost:5016
```

Nếu frontend đã build/publish cùng backend, mở:

```text
http://localhost:5016/products
```

Nếu chạy Vite dev:

```powershell
cd src/JiraIntegrationService.Web
npm install
npm run dev
```

Mở:

```text
http://localhost:5173/products
```

Vào màn hình `Settings` và kiểm tra:

| Field | Giá trị dev mặc định | Ghi chú |
| --- | --- | --- |
| API base URL | `/api` | Dùng khi chạy Vite proxy. Nếu cần có thể đổi sang `http://localhost:5016/api`. |
| Internal auth token | `dev-internal-token` | UI sẽ gửi vào header `X-Internal-Auth`. |

Nếu gọi API trực tiếp bằng Postman/curl/PowerShell, thêm header:

```text
X-Internal-Auth: dev-internal-token
```

## 2. Thứ Tự Cấu Hình Product Mới

Thứ tự khuyến nghị:

1. Tạo `Product`.
2. Cấu hình `Credential`.
3. Tạo `Issue Type`.
4. Tạo `Field Mapping`.
5. Tạo `Status Mapping`.
6. Chạy `Validate`.
7. Chạy `Create issue preview`.
8. Tạo issue thật trên Jira.

Lý do nên đi đúng thứ tự này: các cấu hình sau phụ thuộc vào cấu hình trước. Ví dụ field mapping cần issue type, status mapping cần issue type, validate cần product active và credential active.

## 3. Product

Product là cấu hình gốc cho một sản phẩm/hệ thống tích hợp.

Vào:

```text
Products -> New product
```

Hoặc sửa product:

```text
Products -> chọn product -> Overview -> Edit product
```

| Field | Bắt buộc | Ví dụ | Ghi chú |
| --- | --- | --- | --- |
| Product code | Có | `CRM`, `OPS` | Backend chuẩn hóa thành uppercase. Dùng trong request `productCode`. |
| Name | Có | `CRM System` | Tên để admin nhận diện. |
| Jira project key | Có | `CRM` | Project key trên Jira. |
| Jira base URL | Có | `https://jira.company.local` | Không cần thêm `/rest/api/2`; backend tự ghép với API path. |
| Jira API path | Không | `/rest/api/2` | Nếu để trống, backend mặc định `/rest/api/2`. |
| Jira version | Không | `ServerV2` | Hiện tại nên dùng `ServerV2`; để sẵn cho version sau. |
| Active | Có | `true` | Product inactive sẽ không được service nghiệp vụ sử dụng. |

Lưu ý:

- `Product code` không có API update trực tiếp. Nếu cần đổi code, nên tạo product mới hoặc xóa/tạo lại.
- `Jira base URL` nên gồm scheme `http://` hoặc `https://`.
- Backend trim dấu `/` cuối của `Jira base URL`.

API mẫu:

```http
POST /api/admin/products
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "code": "OPS",
  "name": "Operations",
  "jiraProjectKey": "OPS",
  "jiraBaseUrl": "https://jira.company.local",
  "jiraApiBasePath": "/rest/api/2",
  "jiraVersion": "ServerV2",
  "isActive": true
}
```

## 4. Jira Credential

Credential là tài khoản/token dùng để backend gọi Jira theo từng product.

Vào:

```text
Products -> chọn product -> Credential
```

| Field | Bắt buộc | Ví dụ | Ghi chú |
| --- | --- | --- | --- |
| Auth type | Có | `Basic` | Hiện tại backend chỉ hỗ trợ `Basic`. |
| Username | Có | `jira-bot` | Username Jira hoặc email tùy Jira server. |
| Password or token | Có khi save | `***` | Backend không trả lại secret. Mỗi lần save credential phải nhập lại password/token. |
| Active | Có | `true` | Create/update status chỉ dùng credential active. |

Lưu ý bảo mật:

- API `GET credential` chỉ trả `hasPasswordOrToken`, không trả secret.
- Secret hiện đang lưu trong SQLite bảng `JiraCredentials`. Chưa có mã hóa secret ở phase hiện tại.
- Nếu có nhiều credential active trong DB cho cùng product, service nghiệp vụ yêu cầu chỉ có một credential active. Admin nên giữ duy nhất một credential active.

API mẫu:

```http
PUT /api/admin/products/OPS/credential
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "authType": "Basic",
  "username": "jira-bot",
  "passwordOrToken": "jira-password-or-token",
  "isActive": true
}
```

## 5. Issue Type

Issue type mapping cho biết issue type nội bộ của hệ thống sẽ tạo thành issue type nào trên Jira.

Vào:

```text
Products -> chọn product -> Issue Types
```

| Field | Bắt buộc | Ví dụ | Ghi chú |
| --- | --- | --- | --- |
| Issue type code | Có | `BUG`, `TASK`, `INCIDENT` | Backend chuẩn hóa uppercase. Dùng trong request `issueTypeCode`. |
| Jira issue type id | Không | `10004` | Nếu có, backend ưu tiên gửi `issuetype.id`. |
| Jira issue type name | Không | `Bug`, `Task` | Dùng khi không có ID. |
| Active | Có | `true` | Issue type inactive sẽ không được create issue sử dụng. |

Bắt buộc phải có ít nhất một trong hai field:

- `Jira issue type id`
- `Jira issue type name`

Khuyến nghị:

- Nếu Jira issue type có ID ổn định và admin biết ID, dùng ID để tránh sai do đổi tên issue type.
- Nếu chưa có ID, dùng name đúng y hệt trong Jira, ví dụ `Bug`.

API mẫu:

```http
POST /api/admin/products/OPS/issue-types
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "issueTypeCode": "INCIDENT",
  "jiraIssueTypeId": null,
  "jiraIssueTypeName": "Incident",
  "isActive": true
}
```

## 6. Field Mapping

Field mapping là phần quan trọng nhất. Nó nói backend lấy dữ liệu từ request ở đâu và đẩy sang Jira field nào.

Vào:

```text
Products -> chọn product -> Field Mappings
```

Chọn issue type trước, sau đó bấm `New mapping`.

### 6.1. Cách Backend Build Payload

Request tạo issue có dạng:

```json
{
  "productCode": "OPS",
  "issueTypeCode": "INCIDENT",
  "data": {
    "summary": "Payment job failed",
    "description": "Nightly payment job failed at 02:00",
    "priority": "High",
    "customer": {
      "code": "CUST-001"
    },
    "components": ["Backend", "Payment"]
  }
}
```

Nếu mapping:

| Source path | Jira field | Value type | Value shape |
| --- | --- | --- | --- |
| `data.summary` | `summary` | `string` | `raw` |
| `data.description` | `description` | `string` | `raw` |
| `data.priority` | `priority` | `string` | `name` |
| `data.customer.code` | `customfield_10010` | `string` | `raw` |
| `data.components` | `components` | `array` | `arrayOfName` |

Backend sẽ build Jira payload tương đương:

```json
{
  "fields": {
    "project": { "key": "OPS" },
    "issuetype": { "name": "Incident" },
    "summary": "Payment job failed",
    "description": "Nightly payment job failed at 02:00",
    "priority": { "name": "High" },
    "customfield_10010": "CUST-001",
    "components": [
      { "name": "Backend" },
      { "name": "Payment" }
    ]
  }
}
```

### 6.2. Field Mapping Fields

| Field | Bắt buộc | Ví dụ | Ghi chú |
| --- | --- | --- | --- |
| Source path | Có | `data.summary`, `summary`, `data.customer.code` | Backend resolve case-insensitive. Prefix `data.` có hoặc không đều được. |
| Jira field | Có | `summary`, `description`, `customfield_10010` | Tên field Jira REST API. |
| Value type | Không | `string` | Mặc định `string`. |
| Value shape | Không | `raw` | Mặc định `raw`. |
| Required | Có | `true` | Nếu missing/null/empty thì create issue bị từ chối. |
| Default value | Không | `Medium` | Dùng khi source path không tồn tại, null hoặc undefined. Chuỗi rỗng không tự fallback về default. |
| Sort order | Có | `10`, `20` | Thứ tự build và hiển thị trên UI. |
| Active | Có | `true` | Mapping inactive không được sử dụng. |
| Transform config JSON | Không | `{ "type": "template" }` | Hiện tại chỉ lưu, chưa áp dụng transform. |

Lưu ý quan trọng:

- Phải có ít nhất một mapping active đến Jira field `summary`.
- Mapping đến `summary` phải tạo ra giá trị khác rỗng.
- Trong cùng issue type, không nên có hai mapping active cùng đẩy vào một `Jira field`.
- `Source path` có thể là nested path, ví dụ `data.customer.code`.
- Backend resolve path không phân biệt hoa thường, ví dụ `data.Summary` vẫn đọc được `summary`.
- UI khi tạo issue sẽ hiện form dựa trên các mapping active của issue type.

### 6.3. Value Type

| Value type | Đầu vào hợp lệ | Ghi chú |
| --- | --- | --- |
| `string` | string, number, boolean, object, array | Backend chuyển thành string/raw JSON text nếu cần. |
| `date` | string/date từ UI | Backend xử lý như string. UI dùng format `YYYY-MM-DD`. |
| `number` | number hoặc string parse được number | Parse theo invariant culture, ví dụ `10.5`. |
| `boolean` | boolean hoặc string `true`/`false` | Không chấp nhận `1`/`0`. |
| `object` | JSON object | Nếu input là string JSON thì backend không tự parse lại. |
| `array` | JSON array | Cần dùng với `arrayOfName` hoặc `arrayOfId`. |

Default value trong phase hiện tại phù hợp nhất với `string`, `date`, `number`, `boolean`. Default value chỉ được dùng khi source path không tồn tại/null/undefined; nếu client gửi chuỗi rỗng thì backend xem là giá trị rỗng và có thể báo lỗi required. Với `object`/`array`, nên gửi giá trị thật trong request thay vì dựa vào default value, vì backend đang lưu default như string.

### 6.4. Value Shape

| Value shape | Jira payload tạo ra | Dùng cho |
| --- | --- | --- |
| `raw` | `"field": value` | `summary`, `description`, text custom field, number custom field. |
| `name` | `"field": { "name": value }` | `priority`, user/option/component theo name nếu Jira chấp nhận. |
| `id` | `"field": { "id": value }` | Option/custom field/issue type cần ID. |
| `value` | `"field": { "value": value }` | Select list custom field theo value. |
| `arrayOfName` | `"field": [{ "name": "A" }]` | Components/labels dạng object name. Cần `valueType = array`. |
| `arrayOfId` | `"field": [{ "id": "10001" }]` | Multi select theo ID. Cần `valueType = array`. |

Ví dụ mapping priority:

```json
{
  "sourcePath": "data.priority",
  "jiraField": "priority",
  "valueType": "string",
  "valueShape": "name",
  "isRequired": false,
  "defaultValue": "Medium",
  "sortOrder": 30,
  "isActive": true
}
```

Ví dụ mapping custom field:

```json
{
  "sourcePath": "data.customer.code",
  "jiraField": "customfield_10010",
  "valueType": "string",
  "valueShape": "raw",
  "isRequired": true,
  "defaultValue": null,
  "sortOrder": 40,
  "isActive": true
}
```

API mẫu:

```http
POST /api/admin/products/OPS/issue-types/INCIDENT/field-mappings
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "sourcePath": "data.summary",
  "jiraField": "summary",
  "valueType": "string",
  "valueShape": "raw",
  "isRequired": true,
  "defaultValue": null,
  "sortOrder": 10,
  "isActive": true,
  "transformConfigJson": null
}
```

## 7. Status Mapping

Status mapping dùng cho hai luồng:

- `POST /api/issues/status/update`: map status chuẩn nội bộ sang Jira transition.
- `GET /api/issues/status`: lấy Jira status và map ngược về status chuẩn nội bộ.

Vào:

```text
Products -> chọn product -> Status Mappings
```

Chọn issue type trước, sau đó bấm `New status`.

| Field | Bắt buộc | Ví dụ | Ghi chú |
| --- | --- | --- | --- |
| Standard status | Có | `OPEN`, `IN_PROGRESS`, `WAITING`, `DONE`, `CANCELLED` | Luồng update chỉ chấp nhận các status này. |
| Jira status name | Có | `In Progress`, `Done` | Tên status thật sau khi transition thành công. |
| Jira transition id | Không | `31` | Backend ưu tiên match transition theo ID. |
| Jira transition name | Không | `Start Progress` | Dùng fallback nếu không có ID. |
| Active | Có | `true` | Mapping inactive không được sử dụng. |

Luồng update status chạy như sau:

```text
Client gửi productCode + issueTypeCode + jiraIssueId/jiraIssueKey + standardStatus
-> Backend đọc StatusMapping
-> Backend gọi Jira GET available transitions của issue hiện tại
-> Chọn transition theo JiraTransitionId, nếu không có thì theo JiraTransitionName
-> POST transition lên Jira
-> GET lại status thật từ Jira
-> So sánh Jira status thật với JiraStatusName đã cấu hình
-> Nếu khớp thì trả thành công, nếu lệch thì báo lỗi config/transition
```

Lưu ý:

- `Jira status name` không phải transition name. Đây là status sau cùng mong đợi sau khi transition xong.
- `Jira transition id` phụ thuộc vào workflow và trạng thái hiện tại của issue. Cùng một status đích có thể có transition ID khác nhau theo workflow.
- Nếu transition không nằm trong danh sách available transitions của issue hiện tại, backend sẽ báo lỗi Jira transition is not available.
- Nếu transition thành công nhưng status sau đó không đúng `Jira status name`, backend sẽ báo lỗi mismatch để admin sửa config.

API mẫu:

```http
POST /api/admin/products/OPS/issue-types/INCIDENT/status-mappings
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "standardStatus": "IN_PROGRESS",
  "jiraStatusName": "In Progress",
  "jiraTransitionId": "31",
  "jiraTransitionName": "Start Progress",
  "isActive": true
}
```

## 8. Validate Cấu Hình

Sau khi cấu hình xong, vào:

```text
Products -> chọn product -> Validate
```

Chọn issue type hoặc để all issue types, bấm `Validate`.

Backend sẽ kiểm tra:

- Product active.
- Product có `JiraProjectKey`, `JiraBaseUrl`, `JiraApiBasePath`, `JiraVersion`.
- Có credential active.
- Credential có `AuthType`, `Username`, `PasswordOrToken`.
- Có issue type active.
- Issue type có `JiraIssueTypeId` hoặc `JiraIssueTypeName`.
- Có mapping active đến Jira field `summary`.
- Required mapping có `SourcePath` hoặc `DefaultValue`.
- Không có duplicate Jira field trong effective mappings của issue type.

API mẫu:

```http
POST /api/admin/products/OPS/validate-create-issue-config
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "issueTypeCode": "INCIDENT"
}
```

Response thành công:

```json
{
  "success": true,
  "data": {
    "productCode": "OPS",
    "issueTypeCode": "INCIDENT",
    "isValid": true,
    "errors": []
  },
  "traceId": "..."
}
```

Response có lỗi cấu hình:

```json
{
  "success": true,
  "data": {
    "productCode": "OPS",
    "issueTypeCode": "INCIDENT",
    "isValid": false,
    "errors": [
      "Active Jira credential is required.",
      "Issue type 'INCIDENT' must have a mapping to Jira field 'summary'."
    ]
  },
  "traceId": "..."
}
```

## 9. Preview Create Issue

Nên chạy preview trước khi tạo issue thật.

Preview build Jira request từ config và data đầu vào, nhưng không gọi Jira.

API:

```http
POST /api/issues/create/preview
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "productCode": "OPS",
  "issueTypeCode": "INCIDENT",
  "data": {
    "summary": "Payment job failed",
    "description": "Nightly payment job failed at 02:00",
    "priority": "High",
    "customer": {
      "code": "CUST-001"
    }
  }
}
```

Nếu preview đúng, mới gọi tạo issue thật:

```http
POST /api/issues/create
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "productCode": "OPS",
  "issueTypeCode": "INCIDENT",
  "data": {
    "summary": "Payment job failed",
    "description": "Nightly payment job failed at 02:00",
    "priority": "High",
    "customer": {
      "code": "CUST-001"
    }
  }
}
```

Response:

```json
{
  "success": true,
  "data": {
    "jiraIssueId": "10001",
    "jiraIssueKey": "OPS-123"
  },
  "traceId": "..."
}
```

## 10. Cập Nhật Và Lấy Trạng Thái Issue

Cập nhật status:

```http
POST /api/issues/status/update
X-Internal-Auth: dev-internal-token
Content-Type: application/json
```

```json
{
  "productCode": "OPS",
  "issueTypeCode": "INCIDENT",
  "jiraIssueKey": "OPS-123",
  "standardStatus": "IN_PROGRESS"
}
```

Lưu ý:

- Cần có `jiraIssueId` hoặc `jiraIssueKey`.
- Nếu gửi cả hai, backend ưu tiên `jiraIssueId` khi gọi Jira.
- `standardStatus` không được là `UNKNOWN`.
- `standardStatus` update hiện chỉ chấp nhận: `OPEN`, `IN_PROGRESS`, `WAITING`, `DONE`, `CANCELLED`.

Lấy status:

```http
GET /api/issues/status?productCode=OPS&issueTypeCode=INCIDENT&jiraIssueKey=OPS-123
X-Internal-Auth: dev-internal-token
```

Response:

```json
{
  "success": true,
  "data": {
    "standardStatus": "IN_PROGRESS"
  },
  "traceId": "..."
}
```

Nếu Jira status không map được về status chuẩn, backend trả:

```json
{
  "standardStatus": "UNKNOWN"
}
```

## 11. Checklist Cấu Hình Product Mới

Dùng checklist này trước khi bàn giao product mới:

- [ ] Product code đúng và đang active.
- [ ] Jira base URL đúng environment.
- [ ] Jira API path là `/rest/api/2`.
- [ ] Jira version là `ServerV2`.
- [ ] Jira project key đúng với project trên Jira.
- [ ] Credential active và token/password mới nhất.
- [ ] Issue type active và có Jira issue type ID/name.
- [ ] Field mapping có `summary`, required và active.
- [ ] Các required field khác có source path đúng hoặc default value.
- [ ] Các custom field đúng ID REST API, ví dụ `customfield_10010`.
- [ ] `priority`, `components`, select list dùng `valueShape` phù hợp.
- [ ] Không duplicate Jira field trong cùng issue type.
- [ ] Status mapping active cho các status cần update.
- [ ] Transition ID/name tồn tại trong workflow của issue.
- [ ] `Jira status name` đúng với status thật sau transition.
- [ ] Validate pass.
- [ ] Create issue preview đúng payload mong đợi.
- [ ] Tạo issue test thành công trên Jira.
- [ ] Update status test thành công với issue test.
- [ ] Get status map về standard status đúng.

## 12. Lỗi Thường Gặp

| Dấu hiệu | Nguyên nhân hay gặp | Cách xử lý |
| --- | --- | --- |
| `Active product config was not found` | Product code sai hoặc product inactive | Kiểm tra product code và Active. |
| `Active Jira credential is required` | Chưa có credential active | Vào tab Credential, nhập username/password-token và bật Active. |
| `A mapping to Jira field 'summary' is required` | Thiếu mapping summary | Tạo mapping `data.summary -> summary`. |
| `Field 'data.xxx' is required` | Request không có data tại source path required, hoặc gửi chuỗi rỗng | Sửa request data hoặc thêm default value nếu path có thể bị thiếu/null. |
| `valueType ... is not supported` | Value type sai chính tả | Chỉ dùng `string`, `number`, `boolean`, `date`, `object`, `array`. |
| `valueShape ... is not supported` | Value shape sai chính tả | Chỉ dùng `raw`, `name`, `id`, `value`, `arrayOfName`, `arrayOfId`. |
| `Value for '...' must be array` | Dùng `arrayOfName/arrayOfId` nhưng data không phải array | Đổi valueType thành `array` và gửi array trong data. |
| Jira trả lỗi field không tồn tại | Sai `customfield_xxxxx` hoặc field không nằm trên screen create | Kiểm tra Jira field ID và create screen của project/issue type. |
| Jira transition is not available | Transition ID/name không available ở status hiện tại | Kiểm tra workflow và available transitions của issue. |
| Status sau transition bị mismatch | `Jira status name` cấu hình không đúng status thật | Sửa `Jira status name` theo status Jira trả về sau transition. |
| API trả `AUTH_ERROR` | Thiếu/sai `X-Internal-Auth` | Kiểm tra Settings UI hoặc header request. |

## 13. Ghi Chú Cho Phase Sau

Thiết kế hiện tại có sẵn cho các nâng cấp sau:

- Hỗ trợ version khác bằng `jiraVersion`, ví dụ Jira Cloud.
- Thêm auth type khác ngoài `Basic`.
- Thêm product-level status mapping trong Admin API/UI.
- Áp dụng `TransformConfigJson` cho mapping mức B:
  - template
  - concat
  - enum map
  - date format
  - condition
  - fallback chain
- Mã hóa credential trong DB.
- Audit log thay đổi cấu hình admin.
## 14. Jira Field Metadata Va Description Tu Nhap

Tab `Field Mappings` doc field metadata tu cache trong DB theo product va issue type.
Neu can cap nhat lai tu Jira, bam `Reload Jira fields`. UI se hien thoi diem
cap nhat cuoi cung cua cache.

Dieu kien:

- Product co credential active.
- Issue type co `Jira issue type id`.
- Product cau hinh dung `Jira project key`, `Jira base URL`, `Jira API path`.

Luong khuyen nghi:

1. Vao `Products -> chon product -> Issue Types`.
2. Sync issue types tu Jira hoac nhap `Jira issue type id` thu cong.
3. Vao tab `Field Mappings`, chon issue type.
4. Bam `Reload Jira fields` de sync metadata tu Jira vao cache.
5. Khi tao/sua mapping, chon `Jira metadata` de UI tu dien `Jira field`, `Jira field name`, `Description`, `Source path`, `Value type`, `Value shape`, required/default/allowed values neu Jira tra ve.

Field `Description` trong mapping mac dinh lay bang `Jira field name` khi tao mapping moi,
admin co the sua lai neu can mo ta chi tiet hon. Mo ta nay duoc hien o man
`Create issue` de nguoi tao issue nhap dung hon.

Voi select custom field, admin nen dung:

| Jira schema | Value type | Value shape | Default value |
| --- | --- | --- | --- |
| `option` | `string` | `value` | Gia tri option, vi du `Development` |
| `priority` | `string` | `name` | Ten priority, vi du `Medium` |
| `array:component` | `array` | `raw` voi `jiraField = componentIds` | JSON array id, vi du `["15690"]` |
| `array:version` | `array` | `arrayOfId` | JSON array id version |

Man `Create issue` se hien select/multi-select neu mapping co `JiraAllowedValuesJson`. Neu khong co allowed values, UI fallback ve input/textarea JSON nhu truoc.
