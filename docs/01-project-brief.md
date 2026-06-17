# Project Brief

## Tên dự án

**Jira Integration Service**

## Mục tiêu

Xây dựng một backend/service nội bộ dùng chung để các sản phẩm trong công ty tích hợp với Jira mà không phải gọi trực tiếp Jira API.

Mục tiêu chính của MVP là:

- Tạo Jira issue.
- Cập nhật trạng thái Jira issue bằng bộ trạng thái chuẩn nội bộ.
- Lấy trạng thái Jira issue và trả về bộ trạng thái chuẩn nội bộ.
- Hỗ trợ nhiều sản phẩm ngay từ đầu.
- Mỗi sản phẩm map tới một Jira project riêng.
- Che bớt chi tiết Jira workflow khỏi các sản phẩm nội bộ.

## Bối cảnh

Team đang phát triển nhiều sản phẩm nội bộ như CRM, Support, Sales, PMS, POS hoặc các hệ thống khác. Các sản phẩm này có nhu cầu tạo và theo dõi issue trên Jira.

Thay vì nhúng logic Jira vào từng backend riêng lẻ, dự án tách một service trung gian:

```text
Internal Product
  -> Jira Integration Service
  -> Jira REST API
```

Service này giúp tập trung cấu hình Jira, mapping project, mapping issue type, mapping status, logging, retry và xử lý lỗi.

## Client sử dụng service

Các client ban đầu là backend nội bộ của công ty, ví dụ:

- CRM
- Support
- Sales
- PMS
- POS
- Hệ thống nội bộ khác

Mỗi client được định danh bằng `productCode`.

## Phạm vi MVP

MVP cần làm:

- ASP.NET Core Web API chạy nội bộ.
- Sử dụng .NET 10.
- REST API chỉ dùng `GET` và `POST`.
- Auth nội bộ bằng một token cố định cấu hình trong `appsettings`.
- Tạo Jira issue.
- Cập nhật trạng thái Jira issue.
- Lấy trạng thái Jira issue.
- Hỗ trợ định danh issue bằng `jiraIssueId` hoặc `jiraIssueKey`.
- Hỗ trợ nhiều sản phẩm.
- Mỗi sản phẩm có một Jira project riêng.
- Một Jira base URL dùng chung.
- Mỗi sản phẩm có thể có Jira credential riêng bằng `username` và `password`.
- Hỗ trợ nhiều issue type, gồm issue type chung và issue type riêng theo sản phẩm.
- Lưu cấu hình mapping trong SQLite.
- Seed dữ liệu cấu hình ban đầu.
- Log request/response Jira rút gọn vào file.
- Retry 2-3 lần cho lỗi Jira tạm thời.
- Response API có format chuẩn riêng.

## Chưa làm trong MVP

- Jira webhook.
- Queue hoặc message broker.
- Docker/Kubernetes.
- Admin UI.
- API quản lý cấu hình.
- Lưu lịch sử tạo issue hoặc cập nhật trạng thái vào database.
- File đính kèm.
- Đồng bộ trạng thái tự động từ Jira về sản phẩm.
- Auth riêng theo từng product.
- JWT/RBAC.
- Multi Jira base URL.
- Microservice/event-driven architecture phức tạp.

## Thiết kế để mở rộng sau

Các phần sau chưa làm ngay, nhưng code nên đủ rõ ràng để mở rộng:

- Attachment cho Jira issue.
- Audit/history database.
- API quản lý cấu hình.
- Auth riêng theo product.
- Mã hóa Jira credential trong database.
- Admin UI.
- API lấy chi tiết issue.
- Multi Jira base URL.

## Bộ trạng thái chuẩn nội bộ

Bộ status chuẩn tạm thời:

- `OPEN`
- `IN_PROGRESS`
- `WAITING`
- `DONE`
- `CANCELLED`
- `UNKNOWN`

Ghi chú:

- `UNKNOWN` chỉ dùng khi đọc trạng thái từ Jira nhưng không map được.
- Không dùng `UNKNOWN` làm trạng thái đích khi cập nhật issue.

## Giả định hiện tại

- Tất cả sản phẩm dùng chung một Jira base URL.
- Mỗi sản phẩm có thể dùng credential Jira riêng.
- Product gọi service sẽ tự lưu `jiraIssueId` và `jiraIssueKey` sau khi tạo issue thành công.
- SQLite chỉ lưu cấu hình mapping, chưa lưu lịch sử thao tác.
- File log là nguồn debug chính trong MVP.
- Jira Integration Service tự viết client gọi Jira REST API, không dùng Jira SDK.
- API nội bộ nhận auth token qua header `X-Internal-Auth`.

## Nguyên tắc sản phẩm

- MVP nhỏ.
- Dễ chạy nội bộ.
- Dễ debug khi Jira lỗi.
- Dễ tích hợp với nhiều sản phẩm.
- Không over-engineering.
- Code từng bước nhỏ, dễ review.
