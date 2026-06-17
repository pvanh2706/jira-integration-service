# Implementation Plan

File này dùng để AI hoặc developer đánh dấu tiến độ khi code.

Quy ước:

- `[ ]` Chưa làm.
- `[x]` Đã hoàn thành.
- Khi hoàn thành một mục, đổi `[ ]` thành `[x]`.
- Nếu phát sinh thay đổi phạm vi, cập nhật tài liệu liên quan trước hoặc cùng lúc với code.

## Trạng thái tổng quan

- [x] Tạo tài liệu nền tảng cho dự án.
- [x] Scaffold solution .NET 10.
- [x] Implement common API contract.
- [x] Implement auth nội bộ.
- [x] Implement SQLite config database.
- [x] Implement configuration and mapping services.
- [x] Implement Jira REST client.
- [x] Implement API tạo issue.
- [x] Implement API cập nhật status.
- [x] Implement API lấy status.
- [x] Thêm logging, retry và test đủ cho MVP.
- [x] Verify end-to-end local với fake Jira REST server.
- [x] Hoàn thiện handoff notes và README chạy local.

## Phase 0: Documentation

- [x] Tạo project brief.
- [x] Tạo functional specification.
- [x] Tạo technical design.
- [x] Tạo implementation plan có checkbox.

Acceptance criteria:

- AI có thể đọc `docs/README.md` để biết thứ tự đọc.
- Phạm vi MVP và phần chưa làm được ghi rõ.
- Plan có thể tick tiến độ theo từng bước.

## Phase 1: Scaffold solution

- [x] Kiểm tra máy có .NET 10 SDK:

```bash
dotnet --list-sdks
```

- [x] Tạo solution:

```bash
dotnet new sln -n JiraIntegrationService
```

Ghi chú: với .NET 10 CLI, file solution thực tế có thể được tạo là `JiraIntegrationService.slnx`.

- [x] Tạo ASP.NET Core Web API project .NET 10:

```bash
dotnet new webapi -n JiraIntegrationService.Api -o src/JiraIntegrationService.Api --framework net10.0 --use-controllers
```

- [x] Add project vào solution:

```bash
dotnet sln add src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj
```

- [x] Tạo test project:

```bash
dotnet new xunit -n JiraIntegrationService.Tests -o tests/JiraIntegrationService.Tests --framework net10.0
dotnet sln add tests/JiraIntegrationService.Tests/JiraIntegrationService.Tests.csproj
dotnet add tests/JiraIntegrationService.Tests/JiraIntegrationService.Tests.csproj reference src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj
```

- [x] Tạo cấu trúc thư mục chính trong API project:

```text
Controllers/
Application/
Domain/
Infrastructure/
Options/
Common/
```

- [x] Chạy build lần đầu:

```bash
dotnet build
```

- [x] Chạy test scaffold lần đầu:

```bash
dotnet test
```

Acceptance criteria:

- Solution build thành công.
- API project target `net10.0`.
- Web API chạy được local.
- Test project chạy được test scaffold mặc định.
- Chưa cần implement nghiệp vụ Jira.

## Phase 2: Common API contract

- [x] Tạo `ApiResponse<T>`.
- [x] Tạo `ApiErrorResponse`.
- [x] Tạo `ErrorCodes`.
- [x] Tạo helper lấy `traceId`.
- [x] Chuẩn hóa response thành công.
- [x] Chuẩn hóa response lỗi.
- [x] Cấu hình controller validation để lỗi model binding cũng trả response chuẩn, không trả mặc định `ProblemDetails`.
- [x] Tạo global exception handler hoặc middleware để trả `INTERNAL_ERROR`.
- [x] Cài và cấu hình Serilog baseline ghi file rolling theo ngày.
- [x] Thêm `GET /health`.

Acceptance criteria:

- Mọi response có `success` và `traceId`.
- Lỗi không bị trả raw exception ra client.
- Lỗi validation tự động của controller cũng trả format chuẩn.
- Log file được tạo khi service chạy local.
- `GET /health` trả response chuẩn.

Test nên có:

- [x] Test response success format.
- [x] Test response error format.
- [x] Test exception handler trả `INTERNAL_ERROR`.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 4/4 test.
- Chạy local `GET /health` trả response chuẩn.
- Serilog tạo log file dưới `src/JiraIntegrationService.Api/logs/` khi chạy local bằng `dotnet run --project`.

## Phase 3: Internal auth

- [x] Tạo `InternalAuthOptions`.
- [x] Bind cấu hình `InternalAuth:Token` từ `appsettings`.
- [x] Implement middleware hoặc filter kiểm tra header `X-Internal-Auth`.
- [x] Bỏ qua auth cho `GET /health`.
- [x] Trả `AUTH_ERROR` khi thiếu token.
- [x] Trả `AUTH_ERROR` khi sai token.
- [x] Đảm bảo không log token.

Acceptance criteria:

- API nghiệp vụ bị chặn nếu không có token đúng.
- Response lỗi auth theo format chuẩn.

Test nên có:

- [x] `GET /health` không cần auth.
- [x] Request không token bị reject.
- [x] Request sai token bị reject.
- [x] Request đúng token đi tiếp.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 8/8 test.
- Chạy local `GET /health` không cần auth và trả response chuẩn.
- Chạy local `POST /api/issues/create` không token trả `401 AUTH_ERROR`.

## Phase 4: SQLite persistence và seed config

- [x] Cài package EF Core SQLite.
- [x] Tạo `AppDbContext`.
- [x] Tạo entity `Product`.
- [x] Tạo entity `JiraCredential`.
- [x] Tạo entity `IssueTypeMapping`.
- [x] Tạo entity `FieldMapping`.
- [x] Tạo entity `StatusMapping`.
- [x] Cấu hình unique index cần thiết.
- [x] Tạo migration đầu tiên.
- [x] Seed dữ liệu mẫu cho ít nhất một product, ví dụ `CRM`.
- [x] Đảm bảo app tự apply migration khi chạy local development hoặc có hướng dẫn rõ lệnh migrate.

Acceptance criteria:

- SQLite database tạo được.
- Seed data có product, issue type mapping và status mapping mẫu.
- Application đọc được config từ database.

Test nên có:

- [x] Test tìm product theo `productCode`.
- [x] Test tìm issue type mapping.
- [x] Test tìm status mapping.
- [x] Test product inactive không được dùng.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 12/12 test.
- Migration đầu tiên: `InitialCreate`.
- Local tool manifest có `dotnet-ef` version `10.0.9`.
- Chạy local `GET /health` thành công và auto migration tạo SQLite DB tại `src/JiraIntegrationService.Api/jira-integration.db`.

## Phase 5: Configuration and mapping services

- [x] Tạo interface `IProductConfigService`.
- [x] Implement lấy product config từ SQLite.
- [x] Implement lấy Jira credential theo product.
- [x] Implement map `issueTypeCode` sang Jira issue type name.
- [x] Implement map custom fields.
- [x] Implement map `standardStatus` sang Jira transition.
- [x] Implement map Jira status name sang standard status.
- [x] Implement fallback status mapping: issue-type level trước, product-level sau.
- [x] Trả `UNKNOWN` khi đọc status không map được.
- [x] Trả `CONFIG_NOT_FOUND` khi thiếu config bắt buộc.

Acceptance criteria:

- Application service không truy vấn DbContext trực tiếp.
- Mapping service là nơi duy nhất xử lý mapping config.

Test nên có:

- [x] Map issue type thành công.
- [x] Thiếu issue type mapping trả lỗi config.
- [x] Map status update thành công.
- [x] Không có issue-type status mapping thì fallback sang product-level mapping.
- [x] Jira status không map được trả `UNKNOWN`.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 23/23 test.
- `IProductConfigService` đã được đăng ký DI.
- `ConfigNotFoundException` trả `CONFIG_NOT_FOUND` qua global exception middleware.
- Mapping status đọc không tìm thấy trả `UNKNOWN`.

## Phase 6: Jira REST client

- [x] Tạo `JiraOptions` gồm `BaseUrl` và `ApiBasePath`.
- [x] Tạo `IJiraClient`.
- [x] Implement Basic Auth theo credential của product.
- [x] Implement `CreateIssueAsync`.
- [x] Implement `GetIssueStatusAsync`.
- [x] Implement `GetTransitionsAsync`.
- [x] Implement `TransitionIssueAsync`.
- [x] Implement helper tạo `issueIdOrKey`: ưu tiên `jiraIssueId`, fallback `jiraIssueKey`.
- [x] Implement sanitize request/response trước khi log.
- [x] Implement parse lỗi Jira thành `JIRA_ERROR`.
- [x] Cấu hình retry 2-3 lần cho lỗi tạm thời.
- [x] Không retry lỗi 4xx không tạm thời.

Acceptance criteria:

- Jira client không phụ thuộc controller.
- Jira client nhận credential theo từng request/product.
- Jira client log request/response rút gọn.
- Không log password hoặc Authorization header.

Test nên có:

- [x] Create issue parse `id` và `key`.
- [x] Get status parse status name.
- [x] Transition issue gọi đúng endpoint.
- [x] Jira client test bằng fake `HttpMessageHandler` hoặc test double, không gọi Jira thật trong unit test.
- [x] Jira lỗi trả lỗi chuẩn ở application layer.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 29/29 test.
- Jira client dùng `HttpClient`, không dùng Jira SDK.
- Retry mặc định tối đa 3 attempts cho HTTP 429/5xx và lỗi mạng tạm thời.
- 4xx thông thường không retry và được chuẩn hóa thành `JIRA_ERROR`.
- Log request/response đã sanitize các key nhạy cảm và không log password/Authorization header.

## Phase 7: API tạo issue

- [x] Tạo request model `CreateIssueRequest`.
- [x] Tạo response model `CreateIssueResult`.
- [x] Validate `productCode`.
- [x] Validate `issueTypeCode`.
- [x] Validate `summary`.
- [x] Gọi mapping service lấy project, credential, issue type và field mappings.
- [x] Build Jira payload.
- [x] Gọi Jira client tạo issue.
- [x] Trả `jiraIssueId` và `jiraIssueKey`.
- [x] Log action `CreateIssue`.

Acceptance criteria:

- `POST /api/issues/create` hoạt động theo spec.
- Request thiếu field bắt buộc trả `VALIDATION_ERROR`.
- Config thiếu trả `CONFIG_NOT_FOUND`.
- Jira lỗi trả `JIRA_ERROR`.

Test nên có:

- [x] Create issue request hợp lệ.
- [x] Thiếu summary trả validation error.
- [x] Product không tồn tại trả config error.
- [x] Jira create success trả id/key.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 35/35 test.
- `POST /api/issues/create` đã được expose qua `IssuesController`.
- `IssueService` build Jira payload từ product/project, issue type, credential và field mappings.
- Custom fields chỉ gửi các field đã được mapping sang Jira field; hỗ trợ default value và required custom field.
- Request thiếu required field trả `VALIDATION_ERROR`.
- Config thiếu tiếp tục trả `CONFIG_NOT_FOUND` qua global exception middleware.
- Jira create lỗi tiếp tục trả `JIRA_ERROR` từ `IJiraClient`.

## Phase 8: API cập nhật status

- [x] Tạo request model `UpdateIssueStatusRequest`.
- [x] Validate `productCode`.
- [x] Validate có `jiraIssueId` hoặc `jiraIssueKey`.
- [x] Nếu có cả `jiraIssueId` và `jiraIssueKey`, dùng `jiraIssueId` khi gọi Jira.
- [x] Validate `standardStatus`.
- [x] Chặn update sang `UNKNOWN`.
- [x] Gọi mapping service lấy status mapping.
- [x] Gọi Jira client lấy available transitions.
- [x] Match transition theo id trước, name sau.
- [x] Gọi Jira client transition issue.
- [x] Trả response chuẩn.
- [x] Log action `UpdateStatus`.

Acceptance criteria:

- `POST /api/issues/status/update` hoạt động theo spec.
- Hỗ trợ cả `jiraIssueId` và `jiraIssueKey`.
- Không cho update sang `UNKNOWN`.

Test nên có:

- [x] Update bằng issue key.
- [x] Update bằng issue id.
- [x] Gửi cả issue id và issue key thì ưu tiên issue id.
- [x] Thiếu cả id/key trả validation error.
- [x] `UNKNOWN` trả validation error.
- [x] Không có transition mapping trả config error.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 43/43 test.
- `POST /api/issues/status/update` đã được expose qua `IssuesController`.
- Service chỉ truyền `jiraIssueId` xuống Jira client khi request có cả id và key.
- Nếu transition id không có trong available transitions, service fallback match theo transition name.
- Nếu mapping không có cả transition id và name, trả `CONFIG_NOT_FOUND`.
- Nếu Jira không có transition khả dụng phù hợp, trả `JIRA_ERROR`.

## Phase 9: API lấy status

- [x] Tạo query model hoặc bind query parameters.
- [x] Validate `productCode`.
- [x] Validate có `jiraIssueId` hoặc `jiraIssueKey`.
- [x] Hỗ trợ optional `issueTypeCode` để map status chính xác hơn.
- [x] Nếu có cả `jiraIssueId` và `jiraIssueKey`, dùng `jiraIssueId` khi gọi Jira.
- [x] Gọi Jira client lấy issue status.
- [x] Map Jira status name sang standard status.
- [x] Trả `UNKNOWN` nếu không map được.
- [x] Log action `GetStatus`.

Acceptance criteria:

- `GET /api/issues/status` hoạt động theo spec.
- Hỗ trợ cả `jiraIssueId` và `jiraIssueKey`.
- Response chỉ trả standard status nội bộ.

Test nên có:

- [x] Get status bằng issue key.
- [x] Get status bằng issue id.
- [x] Get status với optional `issueTypeCode`.
- [x] Gửi cả issue id và issue key thì ưu tiên issue id.
- [x] Jira status map được trả status chuẩn.
- [x] Jira status không map được trả `UNKNOWN`.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 50/50 test.
- `GET /api/issues/status` đã được expose qua `IssuesController`.
- Query model bind bằng `[FromQuery]`.
- Response chỉ trả `standardStatus`.
- Nếu request có cả id và key, service chỉ truyền `jiraIssueId` xuống Jira client.
- Mapping status dùng `IProductConfigService.MapJiraStatusToStandardStatusAsync`, bao gồm fallback issue-type/product-level và `UNKNOWN`.

## Phase 10: Logging and observability polish

- [x] Rà lại cấu hình Serilog đã tạo ở Phase 2.
- [x] Đảm bảo log file rolling theo ngày.
- [x] Mọi log nghiệp vụ có `traceId`.
- [x] Mọi Jira call log endpoint, method, status code, duration.
- [x] Payload log được truncate/sanitize.
- [x] Error log có `errorCode`.
- [x] Response API trả cùng `traceId` trong log.

Acceptance criteria:

- Khi Jira lỗi, có thể dùng `traceId` trong response để tìm log liên quan.
- Log không chứa secret.

Manual check:

- [ ] Gọi API tạo issue lỗi và tìm được log theo `traceId`.
- [ ] Kiểm tra log không có `X-Internal-Auth`.
- [x] Kiểm tra log không có Jira password.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test` pass 52/52 test.
- Thêm `RequestLogContextMiddleware` để push `TraceId`, `RequestMethod`, `RequestPath` vào Serilog `LogContext`.
- Serilog console/file output template đã in `TraceId` và structured properties.
- `UseSerilogRequestLogging` đã enrich diagnostic context bằng cùng `TraceId`.
- `GlobalExceptionHandlingMiddleware` log cả `ErrorCode` và `TraceId`.
- Jira client log `ErrorCode = JIRA_ERROR` khi Jira call lỗi.
- Jira client test xác nhận log không chứa Jira password, Basic auth value, token/password custom field, hoặc secret response value.
- EF Core log level đã hạ xuống `Warning` để tránh log SQL seed/config có credential.
- Manual check với log runtime thật cho create issue lỗi và `X-Internal-Auth` sẽ kiểm ở Phase 11 khi chạy service local/end-to-end.

## Phase 11: End-to-end local verification

- [x] Chuẩn bị `appsettings.Development.json` mẫu không chứa secret thật.
- [x] Chuẩn bị seed data mẫu.
- [x] Chạy migration.
- [x] Chạy service local.
- [x] Test `GET /health`.
- [x] Test auth sai.
- [ ] Test create issue với Jira test instance.
- [ ] Test update status với Jira test issue.
- [ ] Test get status với Jira test issue.
- [x] Test create issue với fake Jira REST server local.
- [x] Test update status với fake Jira REST server local.
- [x] Test get status với fake Jira REST server local.
- [x] Chạy toàn bộ test:

```bash
dotnet test
```

- [x] Chạy build:

```bash
dotnet build
```

Acceptance criteria:

- Build pass.
- Test pass.
- API MVP chạy được end-to-end với fake Jira REST server local.
- API MVP chưa verify với Jira test instance thật vì chưa có Jira URL/credential thật trong workspace.

Ghi chú kiểm tra:

- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test --no-build` pass 52/52 test.
- `dotnet-ef database update` chạy thành công, database đã up-to-date.
- Service local chạy tại `http://localhost:5116` trong lúc verify.
- Fake Jira REST server local chạy tại `http://localhost:5891` trong lúc verify.
- `GET /health` trả `success=true`, `status=OK`.
- Auth sai trả HTTP 401 với `AUTH_ERROR`.
- Create issue trả `jiraIssueId=10001`, `jiraIssueKey=CRM-123`.
- Update status trả `standardStatus=IN_PROGRESS`.
- Get status trả `standardStatus=IN_PROGRESS`.
- Log runtime tạm chứa traceId của request create/update/get.
- Log runtime tạm không chứa `X-Internal-Auth`.
- Log runtime tạm không chứa Jira password placeholder.
- Đã dừng API process và fake Jira process sau khi verify.

## Phase 12: Handoff notes

- [x] Cập nhật README chạy local ở root repo nếu cần.
- [x] Ghi rõ cách cấu hình `appsettings`.
- [x] Ghi rõ cách chạy migration.
- [x] Ghi rõ cách seed/update config thủ công.
- [x] Ghi rõ các endpoint MVP.
- [x] Ghi rõ phần chưa làm.

Acceptance criteria:

- Developer khác hoặc AI khác có thể đọc docs và chạy service local.
- Implementation plan phản ánh đúng trạng thái hiện tại.

Ghi chú kiểm tra:

- Đã tạo root `README.md` với hướng dẫn chạy local, cấu hình, migration, config thủ công, endpoint MVP, logging và phần chưa làm.
- Đã tạo `docs/05-handoff-notes.md` để tóm tắt trạng thái hiện tại, entry point code và checklist khi có Jira test instance thật.
- Đã cập nhật `docs/README.md` để bỏ trạng thái stale "chưa scaffold/chưa implement" và thêm thứ tự đọc handoff notes.
- Phase 11 vẫn giữ các mục verify với Jira test instance thật ở trạng thái chưa hoàn thành vì chưa có Jira URL/credential thật trong workspace.
- `dotnet build` pass với 0 warning, 0 error.
- `dotnet test --no-build` pass 52/52 test.

## Backlog sau MVP

Chưa code khi chưa có yêu cầu mới:

- [ ] Attachment/file đính kèm.
- [ ] Audit/history database.
- [ ] API quản lý cấu hình.
- [ ] Auth riêng theo product.
- [ ] Mã hóa credential.
- [ ] Admin UI.
- [ ] Jira webhook.
- [ ] Queue/message broker.
- [ ] Docker.
- [ ] Multi Jira base URL.
