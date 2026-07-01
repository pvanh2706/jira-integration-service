# Jira Field Metadata Va Easy Create Issue Plan

Tai lieu nay la plan de AI Code nang cap Jira Integration Service theo muc tieu:

- Admin khong can nho `customfield_xxxxx` khi cau hinh.
- Cau hinh field mapping co them description tu nhap cho custom field/Jira field.
- Man tao issue de dung hon, han che bat user nhap JSON object thu cong.
- Co default value o cau hinh de tao issue nhanh hon.
- Lay metadata field tu Jira theo project + issue type qua API `createmeta`.

## Quyet Dinh Da Chot

- `description` cua custom field/Jira field la thong tin admin tu nhap va luu trong cau hinh.
- Jira API metadata chi dung de lay `fieldId`, `name`, `required`, `schema`, `allowedValues`, `defaultValue`, `autoCompleteUrl`.
- Response mau trong `postman/get-filed-of-issuetype-result.txt` khong co field-level `description`; chi mot so item trong `allowedValues` co description rieng.
- Van giu design configuration-driven hien tai: create issue request la `productCode + issueTypeCode + data`.
- Khong hardcode rieng EAS/SUBTASK trong flow chinh. Preset EAS/SUBTASK co the giu nhu shortcut rieng, nhung metadata sync phai dung duoc cho product/issue type khac.
- Phai can than voi credential trong file curl/postman. Khong them secret moi vao docs, tests, seed, hoac log.

## Input Da Doc

- `postman/curl-get-filed-of-issuetype.json`
  - Goi Jira Server REST API v2:
    - `GET /rest/api/2/issue/createmeta/EAS/issuetypes/6`
- `postman/get-filed-of-issuetype-result.txt`
  - Tong cong `27` fields cho `EAS / issueTypeId=6 / Sub-task`.
  - Required fields: `components`, `issuetype`, `parent`, `project`, `reporter`, `summary`.
  - Custom select fields:
    - `customfield_12815` Technical Issue Type
    - `customfield_14018` Do phuc tap cua Issue
    - `customfield_14338` Activities
    - `customfield_15711` Loai yeu cau ky thuat
    - `customfield_16410` Tieu chi danh gia cong viec
  - Date fields:
    - `customfield_12412`, `customfield_12413`, `customfield_13630`, `duedate`
  - Number fields:
    - `customfield_14025`, `customfield_14028`, `customfield_14029`
  - Large allowed values:
    - `customfield_14338`: 159 options, 91 enabled
    - `fixVersions`: 421 options
  - Jira default examples:
    - `assignee`: `{ "name": "-1" }`
    - `priority`: Medium

## Current State Can Bam Vao

Backend:

- `IJiraClient` da co `GetIssueTypesAsync`.
- `JiraClient` da goi `issue/createmeta/{projectKey}/issuetypes` de sync issue types.
- `AdminConfigurationService` da co:
  - CRUD product/credential/issue type/field mapping/status mapping.
  - `SyncIssueTypesFromJiraAsync`.
  - `SetEasSubTaskDefaultFieldMappingsAsync`.
- `IssueFieldMapping` hien co:
  - `SourcePath`
  - `JiraField`
  - `ValueType`
  - `ValueShape`
  - `IsRequired`
  - `DefaultValue`
  - `SortOrder`
  - `IsActive`
  - `TransformConfigJson`

Frontend:

- `FieldMappingTable.vue` hien dang bat admin nhap tay `Jira field`, `Value type`, `Value shape`, `Default value`.
- `DynamicIssueForm.vue` hien dang render theo field mappings, nhung object/array van la textarea JSON.
- `CreateIssuePage.vue` da co preview payload server-side.

## Data Model De Xuat

### Mo Rong IssueFieldMappings

Them cac cot sau vao bang `IssueFieldMappings`:

```text
JiraFieldName nullable, max 300
JiraFieldDescription nullable, max 2000
JiraSchemaType nullable, max 100
JiraSchemaItems nullable, max 100
JiraSchemaSystem nullable, max 100
JiraSchemaCustom nullable, max 300
JiraAllowedValuesJson nullable, max 20000
JiraDefaultValueJson nullable, max 4000
JiraAutoCompleteUrl nullable, max 1000
```

Ghi chu:

- `JiraFieldDescription` la description tu nhap cua admin.
- Cac cot metadata con lai la snapshot/giai thich tu Jira, giup UI van hien duoc thong tin da chon ngay ca khi khong goi Jira lai.
- Neu muon giam scope, co the chi bat buoc them `JiraFieldName`, `JiraFieldDescription`, `JiraSchemaType`, `JiraSchemaItems`, `JiraAllowedValuesJson`, `JiraDefaultValueJson`. Cac cot khac la nice-to-have.

### Mo Rong Models

Cap nhat cac model sau:

- `UpsertIssueFieldMappingAdminRequest`
  - them `jiraFieldName?: string`
  - them `jiraFieldDescription?: string`
  - them metadata snapshot fields neu co.
- `IssueFieldMappingAdminResponse`
  - tra ve cac field moi de UI render label, helper text, default controls.
- `FieldMappingConfig`
  - them toi thieu `JiraFieldName` va `JiraFieldDescription` de dynamic form hien label/description tot hon.

## Jira Metadata API Noi Bo

### Jira Client

Them model:

```csharp
public sealed record JiraIssueFieldMetadataResponse(
    string FieldId,
    string Name,
    bool Required,
    JiraIssueFieldSchemaResponse Schema,
    bool HasDefaultValue,
    JsonElement? DefaultValue,
    IReadOnlyList<string> Operations,
    IReadOnlyList<JiraAllowedValueResponse> AllowedValues,
    string? AutoCompleteUrl);

public sealed record JiraIssueFieldSchemaResponse(
    string? Type,
    string? Items,
    string? System,
    string? Custom,
    int? CustomId);

public sealed record JiraAllowedValueResponse(
    string? Id,
    string? Key,
    string? Name,
    string? Value,
    string? Description,
    bool Disabled,
    JsonElement Raw);
```

Them method vao `IJiraClient`:

```csharp
Task<IReadOnlyList<JiraIssueFieldMetadataResponse>> GetIssueTypeFieldsAsync(
    JiraConnectionConfig connection,
    string projectKey,
    string issueTypeId,
    CancellationToken cancellationToken = default);
```

Endpoint Jira:

```text
GET issue/createmeta/{projectKey}/issuetypes/{issueTypeId}
```

Mapping response:

- Root `values[]` thanh danh sach field metadata.
- `fieldId` la field REST id can gui khi create issue.
- `name` la label Jira.
- `schema.type/items/system/custom/customId` dung de goi y UI va value shape.
- `allowedValues[]` can filter duoc `disabled`.
- Giu `Raw` cho allowed value de debug, nhung response noi bo nen tra field phang de UI khong phai parse JSON.

### Admin API

Them endpoint:

```http
GET /api/admin/products/{code}/issue-types/{issueTypeCode}/jira-fields
POST /api/admin/products/{code}/issue-types/{issueTypeCode}/jira-fields/sync-from-jira
```

Luog:

```text
AdminConfigurationController
-> AdminConfigurationService.GetJiraFieldsAsync(productCode, issueTypeCode)
-> load product + issue type mapping
-> doc cache JiraIssueTypeFieldMetadata
-> tra response gom updatedAt, total, fields

AdminConfigurationController
-> AdminConfigurationService.SyncJiraFieldsFromJiraAsync(productCode, issueTypeCode)
-> load product + credential + issue type mapping
-> require issueType.JiraIssueTypeId
-> resolve Jira client theo product.JiraVersion
-> jiraClient.GetIssueTypeFieldsAsync(connection, product.JiraProjectKey, issueType.JiraIssueTypeId)
-> normalize/generate recommendedValueType/recommendedValueShape
-> replace cache JiraIssueTypeFieldMetadata
-> tra response cho UI
```

Neu issue type chua co `JiraIssueTypeId`, tra validation error ro:

```text
Jira issue type id is required to reload Jira fields.
```

Ly do: endpoint createmeta theo file mau can issue type id. Neu sau nay can fallback by name, co the map name -> id qua `GetIssueTypesAsync`.

### Admin Response De Xuat

```csharp
public sealed record JiraFieldMetadataAdminResponse(
    string FieldId,
    string Name,
    bool Required,
    string? SchemaType,
    string? SchemaItems,
    string? SchemaSystem,
    string? SchemaCustom,
    int? SchemaCustomId,
    bool HasDefaultValue,
    string? DefaultValueJson,
    string? AutoCompleteUrl,
    IReadOnlyList<string> Operations,
    IReadOnlyList<JiraAllowedValueAdminResponse> AllowedValues,
    string RecommendedValueType,
    string RecommendedValueShape,
    DateTime? UpdatedAt);

public sealed record JiraFieldsMetadataAdminResponse(
    string ProductCode,
    string IssueTypeCode,
    DateTime? UpdatedAt,
    int Total,
    IReadOnlyList<JiraFieldMetadataAdminResponse> Fields);
```

## Recommended Value Mapping

Dung helper rieng, test ky.

Bang goi y:

| Jira schema | valueType | valueShape | Ghi chu |
| --- | --- | --- | --- |
| `string` | `string` | `raw` | summary, description |
| `date` | `date` | `raw` | YYYY-MM-DD |
| `number` | `number` | `raw` | custom float |
| `option` | `string` | `value` | select custom field, UI cho chon option.value |
| `priority` | `string` | `name` | priority theo name |
| `user` | `string` | `name` | reporter/assignee theo username/name tuy Jira server |
| `issuelink` + parent | `string` | `raw` | mapping noi bo nen dung `parentKey` special field |
| `array:component` | `array` | `arrayOfId` | an toan hon name vi response co id |
| `array:version` | `array` | `arrayOfId` | fixVersions |
| `array:string` | `array` | `raw` | labels la array string |
| `array:attachment` | skip/manual | `raw` | create issue binh thuong khong upload attachment qua fields |
| `array:worklog` | `array` | `raw` | worklogs la special handling hien co |

Special fields hien tai trong `JiraIssuePayloadBuilder`:

- Mapping `summary` -> summary.
- Mapping `description` -> description.
- Mapping `priority` -> priority name.
- Mapping `reporter` -> reporter name.
- Mapping `assignee` -> assignee name.
- Mapping `parentKey` -> parent key.
- Mapping `componentIds` -> components ids.
- Mapping `worklogs` -> update.worklog.

Can can than voi Jira field `parent` va `components`:

- UI co the hien Jira field la `Parent`, nhung khi mapping create issue nen goi y `jiraField = parentKey`, `sourcePath = data.parentKey`, `valueShape = raw`.
- UI co the hien Jira field la `Component/s`, nhung khi mapping theo current builder nen goi y `jiraField = componentIds`, `sourcePath = data.componentIds`, `valueType = array`, `valueShape = raw`.
- Neu muon dung truc tiep `fields["components"]` trong CustomFields thi can sua builder/client. De giam risk, phase dau nen giu special fields hien co.

## UX Plan

### Field Mapping Table

Them query:

```ts
adminApi.getJiraFields(productCode, issueTypeCode)
```

Toolbar:

- Nut `Load Jira fields` hoac tu load khi da chon issue type.
- Hien warning neu issue type chua co Jira issue type id.
- Hien count metadata loaded.

Table columns nen co:

- Sort order
- Source path
- Jira field label: `jiraFieldName || jiraField`
- Jira field id: `jiraField`
- Description: `jiraFieldDescription`
- Type/shape
- Required
- Default
- Status
- Action

Create/edit dialog:

- `Jira field`: `el-select` filterable, option label dang `Technical Issue Type (customfield_12815)`.
- Khi chon Jira field:
  - fill `jiraField`
  - fill `jiraFieldName`
  - fill `valueType`
  - fill `valueShape`
  - fill `isRequired`
  - fill metadata snapshot
  - goi y `sourcePath`
  - neu Jira co default value thi hien nut `Use Jira default`
- `Description`: textarea admin tu nhap, placeholder vi du `Dung de phan loai cong viec ky thuat`.
- `Default value`: render theo metadata:
  - option/priority/component/version: select hoac multi-select
  - date: date picker
  - number: input number
  - boolean: switch
  - raw string: input
  - object/array fallback: textarea JSON
- Neu allowed values co `disabled = true`, mac dinh an hoac disable, co toggle `Show disabled options` neu can.

Goi y source path:

- `summary` -> `data.summary`
- `description` -> `data.description`
- `priority` -> `data.priority`
- `reporter` -> `data.reporter`
- `assignee` -> `data.assignee`
- `parent` -> `data.parentKey`
- `components` -> `data.componentIds`
- `customfield_12815` + name `Technical Issue Type` -> `data.technicalIssueType`
- Neu khong chuyen duoc ten thanh camelCase thi fallback `data.customFields.customfield_12815`.

### Dynamic Issue Form

Muc tieu: user tao issue khong can nhap JSON object cho option fields.

Input form nen dung:

- label = `jiraFieldName || jiraField`
- helper text = `jiraFieldDescription`, neu co.
- required = `isRequired`.
- default = `defaultValue`.
- control theo `valueType`, `valueShape`, metadata:
  - `valueShape=value/id/name` va co allowed values: select mot option.
  - `arrayOfId/arrayOfName` va co allowed values: multi-select.
  - `date`: date picker.
  - `number`: input number.
  - `boolean`: switch.
  - `object/array` khong co metadata: textarea JSON fallback.

Output `data` gui backend van la simple value:

```json
{
  "technicalIssueType": "Development",
  "activity": "SX_Development",
  "componentIds": ["15690"]
}
```

Backend se wrap theo `valueShape`.

## Default Values Plan

Default value hien co dang la string trong mapping. Giu lai de giam migration risk.

Quy uoc luu:

- `string/date/number/boolean`: luu scalar string, vi du `Development`, `2026-06-15`, `5`, `true`.
- `array`: luu JSON array string, vi du `["15690"]`.
- `object`: luu JSON object string, chi dung khi bat buoc.

UI phai convert:

- Khi select single option:
  - neu `valueShape=value`: luu option `value`
  - neu `valueShape=id`: luu option `id`
  - neu `valueShape=name`: luu option `name`
- Khi multi-select:
  - luu JSON array cua id/name/value tuy shape.

Backend hien co da parse default JSON cho `object/array` trong `JiraIssuePayloadBuilder.ResolveValue`. Can giu test cho luong nay.

## Backend Implementation Phases

### Phase 1 - Models va Migration

- [x] Them cot metadata/description vao `IssueFieldMapping`.
- [x] Update `AppDbContext.ConfigureIssueFieldMappings`.
- [x] Update `IssueFieldMappingAdminResponse`.
- [x] Update `UpsertIssueFieldMappingAdminRequest`.
- [x] Update `FieldMappingConfig`.
- [x] Update `ProductConfigService.ToFieldMappingConfig`.
- [x] Update `AdminConfigurationService.ApplyFieldMappingRequest`.
- [x] Update `AdminConfigurationService.ToFieldMappingResponse`.
- [x] Tao EF migration.
- [x] Cap nhat seed/default mapping neu can.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx`

### Phase 2 - Jira Field Metadata Client

- [x] Them Jira metadata response models.
- [x] Them `IJiraClient.GetIssueTypeFieldsAsync`.
- [x] Implement trong `JiraClient`.
- [x] Parse `allowedValues` linh hoat cho option/component/version/project/priority/user.
- [x] Giu `Raw` hoac raw JSON an toan cho debug.
- [x] Them tests cho parse response mau hoac mock JSON nho dai dien.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx`

### Phase 3 - Admin Metadata Endpoint

- [x] Them models admin response.
- [x] Them method vao `IAdminConfigurationService`.
- [x] Implement `GetJiraFieldsAsync`.
- [x] Them bang cache `JiraIssueTypeFieldMetadata`.
- [x] Doi `GetJiraFieldsAsync` sang doc cache DB.
- [x] Them `SyncJiraFieldsFromJiraAsync` de user chu dong reload tu Jira.
- [x] Them helper `RecommendFieldMapping`.
- [x] Them endpoint controller:
  - `GET /api/admin/products/{code}/issue-types/{issueTypeCode}/jira-fields`
  - `POST /api/admin/products/{code}/issue-types/{issueTypeCode}/jira-fields/sync-from-jira`
- [x] Them tests controller/service:
  - cached GET khong goi Jira khi chua sync.
  - sync success with issue type id va luu `updatedAt`.
  - sync missing issue type id returns validation error.
  - recommended type/shape cho option/date/number/array component/priority.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx`

### Phase 4 - Persist Metadata Khi Upsert Field Mapping

- [x] UI/backend request co the gui metadata snapshot.
- [x] Backend normalize max length.
- [x] Khong bat buoc metadata snapshot khi tao mapping thu cong.
- [x] Validate `JiraFieldDescription` optional, trim, max length.
- [x] `ValidateCreateIssueConfig` van dung `JiraField`, `SourcePath`, `DefaultValue`; khong phu thuoc metadata.

Verification:

- [x] `dotnet test JiraIntegrationService.slnx`

## Frontend Implementation Phases

### Phase 5 - Types va API Client

- [x] Them TypeScript types:
  - `JiraFieldMetadataAdminResponse`
  - `JiraFieldsMetadataAdminResponse`
  - `JiraAllowedValueAdminResponse`
  - cac field moi trong `IssueFieldMappingAdminResponse`
  - cac field moi trong `UpsertIssueFieldMappingAdminRequest`
- [x] Them `adminApi.getJiraFields(code, issueTypeCode)`.
- [x] Them `adminApi.syncJiraFieldsFromJira(code, issueTypeCode)`.
- [x] Dam bao error message ro neu Jira credential/config sai.

Verification:

- [x] `cd src/JiraIntegrationService.Web && npm run build`

### Phase 6 - Field Mapping UX

- [x] `FieldMappingTable.vue` doc Jira fields cache theo product + issue type.
- [x] Nut `Reload Jira fields` sync metadata tu Jira khi user bam.
- [x] Hien thoi diem cache update lan cuoi.
- [x] Thay input `Jira field` bang select filterable.
- [x] Tu fill `valueType`, `valueShape`, `isRequired`, `jiraFieldName`, metadata snapshot khi chon field.
- [x] Them textarea `Description` tu nhap.
- [x] Hien metadata summary trong dialog:
  - schema
  - required
  - allowed values count
  - default value
- [x] Render default value control theo metadata.
- [x] Giu fallback manual mode neu khong load duoc Jira fields.
- [x] Khong xoa kha nang nhap tay `jiraField` vi co case field dac biet `parentKey`, `componentIds`, `worklogs`.

Verification:

- [x] `cd src/JiraIntegrationService.Web && npm run build`

### Phase 7 - Dynamic Issue Form UX

- [x] `DynamicIssueForm.vue` dung label `jiraFieldName || jiraField`.
- [x] Hien helper description tu `jiraFieldDescription`.
- [x] Render select/multi-select cho mapping co allowed values snapshot.
- [x] Dung default value de prefill control.
- [x] Convert selected values ve data simple theo source path.
- [x] Giu JSON textarea fallback cho object/array khong co metadata.
- [x] Validate required + JSON fallback nhu hien tai.

Verification:

- [x] `cd src/JiraIntegrationService.Web && npm run build`

### Phase 8 - Create Issue Page Polish

- [x] Dam bao preview payload van dung server endpoint hien co.
- [x] Hien request data ro rang sau khi form sinh data.
- [x] Khi create issue loi Jira, message phai giup admin biet field nao sai.
- [x] Them warning neu mapping required nhung khong co default va user chua nhap.

Verification:

- [x] `cd src/JiraIntegrationService.Web && npm run build`
- [x] `dotnet test JiraIntegrationService.slnx`

## Suggested EAS SUBTASK Default Mappings Sau Khi Nang Cap

Nen sua preset EAS/SUBTASK de bot JSON object:

| SourcePath | JiraField | ValueType | ValueShape | Default |
| --- | --- | --- | --- | --- |
| `data.summary` | `summary` | `string` | `raw` | tuy chon |
| `data.description` | `description` | `string` | `raw` | null |
| `data.priority` | `priority` | `string` | `name` | `Medium` |
| `data.reporter` | `reporter` | `string` | `name` | null |
| `data.assignee` | `assignee` | `string` | `name` | `anh.phamviet` |
| `data.parentKey` | `parentKey` | `string` | `raw` | project-specific |
| `data.componentIds` | `componentIds` | `array` | `raw` | `["15690"]` |
| `data.technicalIssueType` | `customfield_12815` | `string` | `value` | `Development` |
| `data.activity` | `customfield_14338` | `string` | `value` | `SX_Development` |
| `data.committedEndDate` | `customfield_13630` | `date` | `raw` | null |
| `data.endDate` | `customfield_12413` | `date` | `raw` | null |
| `data.startDate` | `customfield_12412` | `date` | `raw` | null |
| `data.worklogs` | `worklogs` | `array` | `raw` | optional JSON array |

Neu preset gan default ngay, can tranh default qua cu the nhu parent issue key that khi khong chac dung cho moi user. Tot hon la cho admin sua default sau khi apply preset.

## Tests Bat Buoc

Backend:

- [x] Jira client parse `createmeta/{projectKey}/issuetypes/{issueTypeId}` voi sample response dai dien.
- [x] Parse option allowed values co `id`, `value`, `disabled`.
- [x] Parse component allowed values co `id`, `name`, `description`.
- [x] Recommended mapping cho `option` la `string/value`.
- [x] Recommended mapping cho `priority` la `string/name`.
- [x] Recommended mapping cho `array:component` la `array/arrayOfId` hoac special `componentIds/raw` theo helper.
- [x] Admin endpoint yeu cau issue type id.
- [x] Field mapping description duoc create/update/get dung.
- [x] `FieldMappingConfig` tra description cho create form.
- [x] Payload builder van build dung `valueShape=value`.
- [x] Default array/object van parse dung.

Frontend:

- [x] TypeScript build pass.
- [x] FieldMappingTable build payload upsert co `jiraFieldDescription`.
- [x] Default value select single luu dung scalar.
- [x] Default value multi-select luu dung JSON array.
- [x] DynamicIssueForm render label/description va emit data dung.

Manual smoke:

- [ ] Product EAS co credential active.
- [ ] Sync issue types tu Jira.
- [ ] Chon SUBTASK, reload Jira fields va thay thoi diem update lan cuoi.
- [ ] Tao mapping cho Technical Issue Type bang dropdown, description tu nhap, default `Development`.
- [ ] Create issue form hien label `Technical Issue Type`, select option, khong bat nhap JSON.
- [ ] Preview payload ra `customfield_12815: { "value": "Development" }`.

## Rủi Ro Va Cach Xu Ly

- Jira metadata response rat lon, vi du `fixVersions` 421 items.
  - UI can filterable select, khong render debug JSON dai trong table.
- `allowedValues` shape khac nhau theo field type.
  - Backend flatten thanh `id/key/name/value/description/disabled/rawJson`.
- Field `parent`, `components`, `worklogs` dang co special handling trong builder.
  - Giu special fields `parentKey`, `componentIds`, `worklogs`; UI chi goi y mapping tu Jira field sang special field.
- Jira API khong tra field-level description.
  - Luu `JiraFieldDescription` tu admin, khong overwrite khi reload metadata.
- Working tree hien co dang co nhieu thay doi.
  - Khi code, doc ky diff va khong revert thay doi khong phai cua minh.

## Definition Of Done

- Admin co the doc cache Jira fields theo issue type va chu dong reload tu Jira.
- Admin tao/sua field mapping bang dropdown field, co description tu nhap.
- Field mapping luu duoc metadata snapshot va description.
- Man create issue hien field label + description than thien.
- Cac select field tao issue khong can nhap JSON object thu cong.
- Default value duoc prefill va preview payload dung.
- Backend tests pass.
- Frontend build pass.
- Docs/admin guide duoc cap nhat sau khi implement xong.
