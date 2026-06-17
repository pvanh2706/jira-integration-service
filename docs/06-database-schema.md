# Database Schema

File nay tong hop schema SQLite moi sau Phase 1 cua huong configuration-driven integration.

Database chi luu cau hinh tich hop Jira theo product. Service khong luu lich su request, audit log, issue da tao, webhook event, hay attachment.

## Tong Quan Bang

| Bang | Y nghia ngan gon |
| --- | --- |
| `Products` | Product/he thong noi bo duoc cau hinh de goi Jira. |
| `JiraCredentials` | Credential Jira theo product. |
| `IssueTypeMappings` | Mapping issue type noi bo sang Jira issue type. |
| `IssueFieldMappings` | Mapping du lieu request `data.*` sang Jira fields. |
| `StatusMappings` | Mapping standard status sang Jira status/transition. |
| `__EFMigrationsHistory` | Bang ky thuat cua EF Core. |

## Quan He

```text
Products
  -> JiraCredentials
  -> IssueTypeMappings
       -> IssueFieldMappings
       -> StatusMappings
  -> IssueFieldMappings
  -> StatusMappings
```

Quan he chinh:

- `JiraCredentials.ProductId -> Products.Id`
- `IssueTypeMappings.ProductId -> Products.Id`
- `IssueFieldMappings.ProductId -> Products.Id`
- `IssueFieldMappings.IssueTypeMappingId -> IssueTypeMappings.Id`, nullable
- `StatusMappings.ProductId -> Products.Id`
- `StatusMappings.IssueTypeMappingId -> IssueTypeMappings.Id`, nullable

Khi xoa `Products`, cac record theo `ProductId` bi xoa cascade. Quan he tu `IssueFieldMappings` va `StatusMappings` sang `IssueTypeMappings` dung restrict.

## Products

Bang goc luu cau hinh Jira connection theo product.

| Cot | Kieu | Bat buoc | Y nghia |
| --- | --- | --- | --- |
| `Id` | `INTEGER` | Co | Primary key, auto increment. |
| `Code` | `TEXT`, max 50 | Co | Ma product noi bo, vi du `CRM`. Unique. |
| `Name` | `TEXT`, max 200 | Co | Ten hien thi. |
| `JiraProjectKey` | `TEXT`, max 50 | Co | Project key tren Jira. |
| `JiraBaseUrl` | `TEXT`, max 500 | Co | Base URL Jira cua product. |
| `JiraApiBasePath` | `TEXT`, max 100 | Co | API base path, phase dau la `/rest/api/2`. |
| `JiraVersion` | `TEXT`, max 50 | Co | Version/client key, phase dau la `ServerV2`. |
| `IsActive` | `INTEGER`/boolean | Co | Bat/tat product. |
| `CreatedAt` | `TEXT`/datetime | Co | Thoi diem tao. |
| `UpdatedAt` | `TEXT`/datetime | Co | Thoi diem cap nhat. |

Seed mac dinh:

```text
Code = CRM
Name = CRM
JiraProjectKey = CRM
JiraBaseUrl = https://jira.example.com
JiraApiBasePath = /rest/api/2
JiraVersion = ServerV2
IsActive = true
```

## JiraCredentials

Bang luu credential Jira theo product.

| Cot | Kieu | Bat buoc | Y nghia |
| --- | --- | --- | --- |
| `Id` | `INTEGER` | Co | Primary key, auto increment. |
| `ProductId` | `INTEGER` | Co | Foreign key den `Products.Id`. |
| `AuthType` | `TEXT`, max 50 | Co | Kieu auth. Phase dau dung `Basic`. |
| `Username` | `TEXT`, max 200 | Co | Jira username. |
| `PasswordOrToken` | `TEXT`, max 500 | Co | Password/token placeholder. |
| `IsActive` | `INTEGER`/boolean | Co | Credential dang duoc dung. |
| `CreatedAt` | `TEXT`/datetime | Co | Thoi diem tao. |
| `UpdatedAt` | `TEXT`/datetime | Co | Thoi diem cap nhat. |

Seed mac dinh:

```text
AuthType = Basic
Username = jira-crm-user
PasswordOrToken = change-me
```

Ghi chu bao mat:

- Khong commit credential that.
- Khong log password, token, hoac Authorization header.
- Phase sau nen dung secret manager hoac ma hoa.

## IssueTypeMappings

Bang map issue type noi bo sang issue type Jira.

| Cot | Kieu | Bat buoc | Y nghia |
| --- | --- | --- | --- |
| `Id` | `INTEGER` | Co | Primary key, auto increment. |
| `ProductId` | `INTEGER` | Co | Foreign key den `Products.Id`. |
| `IssueTypeCode` | `TEXT`, max 100 | Co | Ma issue type noi bo, vi du `BUG`. |
| `JiraIssueTypeId` | `TEXT`, max 100 | Khong | Jira issue type id. Uu tien dung neu co. |
| `JiraIssueTypeName` | `TEXT`, max 200 | Khong | Jira issue type name. Fallback khi khong co id. |
| `IsActive` | `INTEGER`/boolean | Co | Bat/tat mapping. |
| `CreatedAt` | `TEXT`/datetime | Co | Thoi diem tao. |
| `UpdatedAt` | `TEXT`/datetime | Co | Thoi diem cap nhat. |

Rang buoc:

- Unique: `ProductId + IssueTypeCode`

Seed mac dinh:

```text
CRM BUG -> Bug
CRM TASK -> Task
```

## IssueFieldMappings

Bang map request `data.*` sang Jira field. Bang nay thay cho `FieldMappings` cu.

| Cot | Kieu | Bat buoc | Y nghia |
| --- | --- | --- | --- |
| `Id` | `INTEGER` | Co | Primary key, auto increment. |
| `ProductId` | `INTEGER` | Co | Foreign key den `Products.Id`. |
| `IssueTypeMappingId` | `INTEGER` | Khong | Mapping theo issue type. Neu `NULL`, la fallback cap product. |
| `SourcePath` | `TEXT`, max 300 | Co | Duong dan trong request, vi du `data.customer.code`. |
| `JiraField` | `TEXT`, max 200 | Co | Field Jira, vi du `summary`, `priority`, `customfield_10010`. |
| `ValueType` | `TEXT`, max 50 | Co | `string`, `number`, `boolean`, `date`, `object`, `array`. |
| `ValueShape` | `TEXT`, max 50 | Co | `raw`, `name`, `id`, `value`, `arrayOfName`, `arrayOfId`. |
| `IsRequired` | `INTEGER`/boolean | Co | Bat buoc co value sau khi tinh default. |
| `DefaultValue` | `TEXT`, max 1000 | Khong | Gia tri mac dinh neu source value thieu/rong. |
| `SortOrder` | `INTEGER` | Co | Thu tu build field. |
| `IsActive` | `INTEGER`/boolean | Co | Bat/tat mapping. |
| `TransformConfigJson` | `TEXT`, max 4000 | Khong | De danh cho transform muc B sau nay. |
| `CreatedAt` | `TEXT`/datetime | Co | Thoi diem tao. |
| `UpdatedAt` | `TEXT`/datetime | Co | Thoi diem cap nhat. |

Rang buoc:

- Unique: `ProductId + IssueTypeMappingId + SourcePath`
- Index: `IssueTypeMappingId`

Seed mac dinh cho `CRM/BUG`:

```text
data.summary       -> summary, raw, required
data.description   -> description, raw
data.priority      -> priority, name
data.customer.code -> customfield_10010, raw
data.ticket.url    -> customfield_10011, raw
```

## StatusMappings

Bang map standard status noi bo sang Jira status/transition.

| Cot | Kieu | Bat buoc | Y nghia |
| --- | --- | --- | --- |
| `Id` | `INTEGER` | Co | Primary key, auto increment. |
| `ProductId` | `INTEGER` | Co | Foreign key den `Products.Id`. |
| `IssueTypeMappingId` | `INTEGER` | Khong | Mapping theo issue type. Neu `NULL`, la fallback cap product. |
| `StandardStatus` | `TEXT`, max 50 | Co | Status noi bo, vi du `IN_PROGRESS`. |
| `JiraStatusName` | `TEXT`, max 200 | Co | Status name thuc te tren Jira. |
| `JiraTransitionId` | `TEXT`, max 100 | Khong | Transition id tren Jira. |
| `JiraTransitionName` | `TEXT`, max 200 | Khong | Transition name fallback. |
| `IsActive` | `INTEGER`/boolean | Co | Bat/tat mapping. |

Rang buoc:

- Unique: `ProductId + IssueTypeMappingId + StandardStatus + JiraStatusName`
- Index: `IssueTypeMappingId`

Seed mac dinh cho `CRM/BUG`:

```text
OPEN        -> To Do
IN_PROGRESS -> In Progress, transition 31 / Start Progress
WAITING     -> Waiting, transition 41 / Waiting
DONE        -> Done, transition 51 / Done
CANCELLED   -> Cancelled, transition 61 / Cancel
```

## Thu Tu Cau Hinh Product Moi

Sau khi co Admin API, product moi nen duoc cau hinh theo thu tu:

1. Tao `Products`.
2. Tao `JiraCredentials`.
3. Tao `IssueTypeMappings`.
4. Tao `IssueFieldMappings`.
5. Tao `StatusMappings`.
6. Chay endpoint validate config.

## Ghi Chu Van Hanh

- Database local mac dinh nam tai `src/JiraIntegrationService.Api/jira-integration.db`.
- Neu app/test server dang chay, file SQLite co the bi lock.
- Co the apply migration thu cong bang:

```powershell
dotnet tool run dotnet-ef database update --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --startup-project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj
```
