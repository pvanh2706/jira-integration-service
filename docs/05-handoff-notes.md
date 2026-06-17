# Handoff Notes

## Configuration Driven Update

- Create issue request hien tai la `productCode + issueTypeCode + data`.
- Field mapping dung bang `IssueFieldMappings` voi `SourcePath`, `JiraField`, `ValueType`, `ValueShape`, `DefaultValue`, `IsRequired`.
- Product moi co the cau hinh qua Admin API duoi `/api/admin/...`.
- Jira connection lay theo product: `Products.JiraBaseUrl`, `Products.JiraApiBasePath`, `Products.JiraVersion` va `JiraCredentials`.
- `status/update` sau khi POST transition se GET lai Jira status va verify voi `StatusMappings.JiraStatusName`.
- Plan nang cap dang duoc theo doi trong `docs/07-configuration-driven-integration-plan.md`.

File này tóm tắt trạng thái hiện tại để AI hoặc developer khác có thể tiếp tục code mà không phải hỏi lại từ đầu.

## Snapshot

- Stack: ASP.NET Core Web API trên .NET 10.
- Persistence: SQLite + EF Core.
- Jira integration: REST API tự viết bằng `HttpClient`, Basic Auth theo credential của product.
- Auth nội bộ: header `X-Internal-Auth`, token lấy từ `InternalAuth:Token`.
- Logging: Serilog console/file, có `TraceId`, sanitize Jira payload.
- Test: unit/integration test bằng xUnit, fake Jira qua test double hoặc fake local server khi verify thủ công.

## Trạng Thái Phase

- Phase 1-10 đã hoàn thành.
- Phase 11 đã pass build/test/migration và end-to-end với fake Jira REST server local.
- Phase 11 còn thiếu verify với Jira test instance thật.
- Phase 12 hoàn tất bằng README root, handoff notes và cập nhật plan.

## Entry Point Code

- API controller: `src/JiraIntegrationService.Api/Controllers/IssuesController.cs`.
- Business service: `src/JiraIntegrationService.Api/Application/Issues/IssueService.cs`.
- Mapping config service: `src/JiraIntegrationService.Api/Infrastructure/Persistence/ProductConfigService.cs`.
- Jira client: `src/JiraIntegrationService.Api/Infrastructure/Jira/JiraClient.cs`.
- EF DbContext và seed: `src/JiraIntegrationService.Api/Infrastructure/Persistence/AppDbContext.cs`.
- Internal auth middleware: `src/JiraIntegrationService.Api/Infrastructure/Security/InternalAuthMiddleware.cs`.
- Global exception middleware: `src/JiraIntegrationService.Api/Common/GlobalExceptionHandlingMiddleware.cs`.
- Request log context middleware: `src/JiraIntegrationService.Api/Infrastructure/Logging/RequestLogContextMiddleware.cs`.

## Config Hiện Tại

Config app nằm trong:

- `src/JiraIntegrationService.Api/appsettings.json`
- `src/JiraIntegrationService.Api/appsettings.Development.json`

SQLite config database mặc định:

```text
src/JiraIntegrationService.Api/jira-integration.db
```

Seed mặc định có product `CRM`, issue types `BUG` và `TASK`, status chuẩn `OPEN`, `IN_PROGRESS`, `WAITING`, `DONE`, `CANCELLED`.

Jira credential thật chưa được commit. Credential placeholder đang nằm ở bảng `JiraCredentials`.

## Cách Verify Nhanh

```powershell
dotnet tool restore
dotnet build
dotnet test
dotnet tool run dotnet-ef database update --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --startup-project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj
dotnet run --project src/JiraIntegrationService.Api/JiraIntegrationService.Api.csproj --launch-profile http
```

Sau khi API chạy:

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5016/health"
```

Các endpoint MVP và ví dụ request chi tiết nằm ở root `README.md`.

## Checklist Khi Có Jira Test Thật

1. Update `Jira:BaseUrl` sang Jira test instance.
2. Update bảng `JiraCredentials` cho product cần test.
3. Kiểm tra `Products.JiraProjectKey` đúng Jira project key.
4. Kiểm tra `IssueTypeMappings.JiraIssueTypeName` đúng tên issue type trên Jira.
5. Kiểm tra `StatusMappings.JiraStatusName`, `JiraTransitionId`, `JiraTransitionName` khớp workflow Jira.
6. Chạy `POST /api/issues/create`.
7. Chạy `POST /api/issues/status/update`.
8. Chạy `GET /api/issues/status`.
9. Kiểm tra log theo `traceId` và xác nhận không lộ secret.
10. Tick các mục Jira test instance còn lại ở phase 11.

## Nguyên Tắc Tiếp Tục Code

- Giữ response chuẩn `success`, `data` hoặc `errorCode/message`, `traceId`.
- Không để controller gọi trực tiếp `AppDbContext` hoặc `HttpClient`.
- Mapping dữ liệu Jira đi qua `IProductConfigService`.
- Jira call đi qua `IJiraClient`.
- Lỗi thiếu config trả `CONFIG_NOT_FOUND`.
- Lỗi Jira trả `JIRA_ERROR`.
- Đọc status không map được thì trả `UNKNOWN`.
- Không log token, password, Authorization header hoặc request header auth nội bộ.

## Backlog Gần Nhất

- Verify với Jira test instance thật.
- Làm config API hoặc công cụ seed/update config chính thức.
- Thêm audit/history database cho request nghiệp vụ.
- Mã hóa credential trong SQLite.
- Tách auth nội bộ theo product nếu cần.
