# Vue Vite Frontend Plan

Plan nay dung de AI/developer tiep tuc xay giao dien quan tri va van hanh cho Jira Integration Service.

## Ket luan da chot

- Frontend dat cung repo tai `src/JiraIntegrationService.Web`.
- Stack: Vue 3 + Vite + TypeScript.
- UI library: Element Plus.
- Lam full UI cho ca quan tri cau hinh va thao tac issue.
- Chua can login.
- Dev chay Vite rieng, production build static va ASP.NET API serve chung mot service.
- Man tao issue dung form dong sinh tu field mapping, kem JSON preview/debug.
- Hien tai tap trung Jira Server REST API v2, nhung thiet ke khong hardcode de sau nay them version khac.
- Lam muc A truoc; note muc B de nang cap sau.

## Pham vi muc A

Muc A la UI quan tri va van hanh dua tren API hien co, them mot vai backend endpoint nho neu can de frontend khong phai lap logic server.

- Quan ly product.
- Quan ly credential theo product.
- Quan ly issue type mapping.
- Quan ly field mapping de tao issue theo cau hinh.
- Quan ly status mapping/transition.
- Validate create issue config.
- Tao issue bang form dong dua tren field mapping.
- Xem va update status issue.
- Serve frontend static tu ASP.NET API khi publish/deploy.

## Note muc B sau nay

Sau muc A, co the nang cap len cac tinh nang sau:

- Dong bo metadata truc tiep tu Jira: projects, issue types, fields, priorities, components, transitions.
- Sinh form tu schema day du hon: labels, descriptions, enum options, nested object, array editor.
- Preview Jira payload chuan hoa kem warning/validation chi tiet hon.
- Import/export/copy cau hinh giua product.
- Audit history cho thay doi cau hinh.
- Login, role/permission, secret management dung chuan hon thay vi token noi bo trong browser.
- Ho tro nhieu Jira version/adapters: Jira Server v2, Jira Cloud v3, adapter theo product.
- Wizard tich hop product moi theo tung buoc.
- Test voi Jira sandbox that va UI e2e automation day du.

## Thu vien de xuat

- `vue`, `vite`, `typescript`.
- `element-plus`, `@element-plus/icons-vue`.
- `vue-router` cho route.
- `pinia` cho UI state/settings nho.
- `@tanstack/vue-query` cho server state, cache, refetch, mutation.
- `axios` cho HTTP client.
- `@vueuse/core` cho local storage/settings.
- `dayjs` cho hien thi thoi gian.
- Dev/test: `vitest`, `@vue/test-utils`, `jsdom`, `eslint`, `prettier`.

Neu can JSON editor dep hon o phase polish, xem xet them CodeMirror. Phase dau co the dung `ElInput type=textarea` + formatted JSON preview de nhe va de bao tri.

## API contract frontend can bam vao

Tat ca API thanh cong tra `ApiResponse<T>`:

```json
{
  "success": true,
  "data": {},
  "traceId": "..."
}
```

API loi tra:

```json
{
  "success": false,
  "errorCode": "...",
  "message": "...",
  "traceId": "..."
}
```

Header hien tai:

```http
X-Internal-Auth: dev-internal-token
```

Luu y bao mat: vi chua co login, token nay neu dung trong frontend se nam o browser/local storage. Chap nhan cho muc A noi bo/dev, nhung muc B nen thay bang auth/permission that.

Endpoint chinh:

- `GET /api/admin/products`
- `POST /api/admin/products`
- `GET /api/admin/products/{code}`
- `PUT /api/admin/products/{code}`
- `DELETE /api/admin/products/{code}`
- `GET /api/admin/products/{code}/credential`
- `PUT /api/admin/products/{code}/credential`
- `GET /api/admin/products/{code}/issue-types`
- `POST /api/admin/products/{code}/issue-types`
- `PUT /api/admin/products/{code}/issue-types/{issueTypeCode}`
- `GET /api/admin/products/{code}/issue-types/{issueTypeCode}/field-mappings`
- `POST /api/admin/products/{code}/issue-types/{issueTypeCode}/field-mappings`
- `PUT /api/admin/field-mappings/{id}`
- `DELETE /api/admin/field-mappings/{id}`
- `GET /api/admin/products/{code}/issue-types/{issueTypeCode}/status-mappings`
- `POST /api/admin/products/{code}/issue-types/{issueTypeCode}/status-mappings`
- `PUT /api/admin/status-mappings/{id}`
- `DELETE /api/admin/status-mappings/{id}`
- `POST /api/admin/products/{code}/validate-create-issue-config`
- `POST /api/issues/create`
- `POST /api/issues/status/update`
- `GET /api/issues/status`

Backend endpoint nen them cho UI preview:

- `POST /api/issues/create/preview`
  - Request giong `POST /api/issues/create`.
  - Dung chung `IJiraIssuePayloadBuilder`.
  - Khong goi Jira.
  - Tra ve payload Jira se duoc gui di, de UI preview/debug khong phai copy mapping engine.

## Router va man hinh

- `/` redirect ve `/products`.
- `/products`
  - Danh sach product.
  - Nut tao product moi.
  - Filter/search nhanh theo code/name/project key.
- `/products/:code`
  - Layout detail theo tabs.
  - Tab `Overview`: thong tin product, Jira base URL, API path, Jira version, active.
  - Tab `Credential`: username, auth type, password/token, active.
  - Tab `Issue Types`: danh sach issue type mapping.
  - Tab `Field Mappings`: CRUD mapping theo issue type.
  - Tab `Status Mappings`: CRUD status/transition theo issue type.
  - Tab `Validate`: goi validate config va hien errors.
- `/issues/create`
  - Chon product.
  - Chon issue type.
  - Sinh form dong tu field mappings.
  - Hien JSON `data` va preview payload.
  - Submit tao Jira issue.
- `/issues/status`
  - Check status issue theo key/id.
  - Update status theo standard status.
  - Hien ket qua sau update.
- `/settings`
  - API base URL cho dev.
  - Internal auth token cho dev/no-login mode.

## Cau truc frontend de xuat

```text
src/JiraIntegrationService.Web/
  index.html
  package.json
  vite.config.ts
  tsconfig.json
  src/
    main.ts
    App.vue
    router/
      index.ts
    stores/
      appSettings.ts
    services/
      http.ts
      adminApi.ts
      issuesApi.ts
    types/
      api.ts
      admin.ts
      issues.ts
    layouts/
      AppShell.vue
    pages/
      ProductsPage.vue
      ProductDetailPage.vue
      CreateIssuePage.vue
      IssueStatusPage.vue
      SettingsPage.vue
    components/
      ProductForm.vue
      CredentialForm.vue
      IssueTypeTable.vue
      FieldMappingTable.vue
      StatusMappingTable.vue
      DynamicIssueForm.vue
      JsonPreview.vue
      ApiResultPanel.vue
      ConfirmDeleteButton.vue
    styles/
      app.scss
```

## Form dong tao issue

Nguon form:

- Lay product tu `GET /api/admin/products`.
- Lay issue types tu `GET /api/admin/products/{code}/issue-types`.
- Lay field mappings tu `GET /api/admin/products/{code}/issue-types/{issueTypeCode}/field-mappings`.

Mapping sang control:

- `valueType = string/date`: `ElInput`; date co the dung `ElDatePicker`.
- `valueType = number`: `ElInputNumber`.
- `valueType = boolean`: `ElSwitch`.
- `valueType = array`: textarea JSON array o muc A.
- `valueType = object`: textarea JSON object o muc A.

Mapping sang request:

```json
{
  "productCode": "CRM",
  "issueTypeCode": "BUG",
  "data": {
    "summary": "Example",
    "description": "Example"
  }
}
```

Quy tac:

- `sourcePath` la key/path trong `data`.
- `isRequired` map sang required rule cua Element Plus.
- `defaultValue` dung de prefill form neu field trong data chua co.
- `sortOrder` dung de sap xep field.
- `jiraField`, `valueType`, `valueShape`, `transformConfigJson` hien o UI debug/admin.
- Luon co warning neu thieu mapping den Jira field `summary`.

## Phase 1 - Scaffold Vue/Vite app

- [x] Tao `src/JiraIntegrationService.Web` bang Vite Vue + TypeScript.
- [x] Cai Element Plus, icons, router, Pinia, TanStack Query, Axios, VueUse, Dayjs.
- [x] Tao `AppShell` voi sidebar/topbar gon, phu hop admin tool.
- [x] Tao router va cac page placeholder.
- [x] Tao global styles, theme mau sang, layout responsive co ban.
- [x] Verify `npm install`, `npm run build`.

## Phase 2 - HTTP client va API types

- [x] Tao `ApiResponse<T>` va `ApiErrorResponse` TypeScript types.
- [x] Tao Axios instance unwrap `data`, map error message/traceId.
- [x] Them `X-Internal-Auth` tu settings/localStorage.
- [x] Cau hinh Vite proxy `/api` ve ASP.NET API khi dev.
- [x] Tao `adminApi.ts` cho admin endpoints.
- [x] Tao `issuesApi.ts` cho create/status endpoints.
- [x] Tao Settings page de sua API base URL/token khi dev.
- [x] Them toast/loading/error conventions cho mutation/query.

## Phase 3 - Backend static serving va preview endpoint

- [x] Cap nhat ASP.NET API de serve static files tu frontend build output.
- [x] Them SPA fallback ve `index.html`.
- [x] Dieu chinh internal auth de static files va SPA routes khong bi chan; API `/api/*` van can token.
- [x] Them `POST /api/issues/create/preview`.
- [x] Preview endpoint dung chung `IJiraIssuePayloadBuilder`, khong goi Jira.
- [x] Them unit/integration tests cho preview endpoint va auth/static route behavior.
- [x] Verify `dotnet test JiraIntegrationService.slnx`.

## Phase 4 - Product administration UI

- [x] Products list: table, search, active badge, open detail.
- [x] Product create/edit dialog.
- [x] Product delete co confirm.
- [x] Product detail overview tab.
- [x] Credential tab: get/upsert credential, password/token input bi mask.
- [x] Hien traceId/error khi API loi.

## Phase 5 - Issue type, field mapping, status mapping UI

- [x] Issue type tab: list/create/update active.
- [x] Field mapping tab:
  - [x] Table theo issue type.
  - [x] Create/edit dialog.
  - [x] Options cho `valueType`: `string`, `number`, `boolean`, `date`, `object`, `array`.
  - [x] Options cho `valueShape`: `raw`, `name`, `id`, `value`, `arrayOfName`, `arrayOfId`.
  - [x] Validate JSON cho `transformConfigJson` neu co nhap.
  - [x] Sort theo `sortOrder`.
- [x] Status mapping tab:
  - [x] Standard status.
  - [x] Jira status name.
  - [x] Transition id/name.
  - [x] Active.
- [x] Validate tab goi `validate-create-issue-config` va hien errors ro rang.

## Phase 6 - Dynamic create issue UI

- [x] Create issue page chon product/issue type.
- [x] Fetch field mappings va sinh form dong.
- [x] Required/default/sortOrder hoat dong dung.
- [x] Sinh JSON `data` tu form.
- [x] Goi preview endpoint de hien Jira payload server-side.
- [x] Submit `POST /api/issues/create`.
- [x] Hien Jira issue id/key va traceId.
- [x] Xu ly loi validation/Jira error de user biet field nao can sua.

## Phase 7 - Issue status UI

- [x] Status page chon product va issue type optional.
- [x] Nhap Jira issue key/id.
- [x] Goi `GET /api/issues/status`.
- [x] Lay status mappings de goi y standard status.
- [x] Goi `POST /api/issues/status/update`.
- [x] Hien ket qua status sau update.
- [x] Hien warning neu status update bi mismatch/verify fail.

## Phase 8 - Polish, docs, verification

- [x] Them empty/loading/error states cho tat ca page chinh.
- [x] Them breadcrumb/page header thao tac nhanh.
- [x] Kiem tra responsive desktop/tablet.
- [x] Chay `npm run build`.
- [x] Chay `dotnet test JiraIntegrationService.slnx`.
- [x] Chay API + Vite dev server, smoke test cac luong chinh.
- [x] Build frontend va verify ASP.NET API serve duoc SPA.
- [x] Cap nhat README cach chay dev/prod frontend.
- [x] Cap nhat docs nay sau moi phase.

## Quy tac khi AI thuc hien

- Lam tung phase mot, cap nhat checkbox sau khi xong.
- Truoc khi sua backend, doc lai controller/model lien quan.
- Khong duplicate mapping engine o frontend neu backend preview endpoint da co.
- Khong hardcode product code, issue type code, Jira version trong UI.
- Khong them login trong muc A.
- Neu can them backend API moi, viet test truoc hoac cung luc voi code.
- Sau moi phase phai co lenh verify ro rang.
