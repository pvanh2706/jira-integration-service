# Jira Integration Service Docs

Update moi: `07-configuration-driven-integration-plan.md` la plan va checklist cho nang cap configuration-driven integration, Admin API, dynamic create issue mapping va Jira connection theo product.

Tài liệu admin: `09-admin-configuration-guide.md` hướng dẫn cấu hình product, credential, issue type, field mapping, status mapping, validate và test tạo issue qua Admin UI/API.

Thư mục này là nguồn tham chiếu chính để AI hoặc developer đọc hiểu và tiếp tục code dự án.

## Thứ Tự Đọc Khuyến Nghị

1. [01-project-brief.md](01-project-brief.md)
   - Hiểu mục tiêu, phạm vi MVP, giả định và phần chưa làm.

2. [02-functional-specification.md](02-functional-specification.md)
   - Hiểu API, request/response, validation, error code, logging và retry.

3. [03-technical-design.md](03-technical-design.md)
   - Hiểu stack, kiến trúc, database schema, Jira REST API client và cấu hình.

4. [04-implementation-plan.md](04-implementation-plan.md)
   - Kế hoạch code chi tiết theo từng phase.
   - File này dùng checkbox để đánh dấu tiến độ.

5. [05-handoff-notes.md](05-handoff-notes.md)
   - Tóm tắt trạng thái hiện tại, cách verify nhanh và checklist khi có Jira test thật.

Root [README.md](../README.md) là tài liệu chạy local và sử dụng endpoint MVP.

## Quy Tắc Khi AI Code Dự Án

- Luôn đọc các tài liệu trên trước khi code.
- Chỉ code theo phạm vi MVP hoặc phase tiếp theo đã được user đồng ý.
- Không tự thêm webhook, queue, admin UI, Docker, attachment hoặc audit history khi chưa được yêu cầu.
- Sau mỗi nhóm việc hoàn thành, cập nhật checkbox trong `04-implementation-plan.md`.
- Nếu phải thay đổi quyết định kỹ thuật, cập nhật tài liệu tương ứng trước hoặc cùng lúc với code.
- Giữ thay đổi nhỏ, dễ review, có thể chạy và kiểm thử từng bước.

## Trạng Thái Hiện Tại

- Đã hoàn thành MVP từ phase 1 đến phase 10.
- Đã scaffold source code .NET 10, tạo migration SQLite, implement API create/update/get status.
- Đã implement internal auth, common API response, mapping service, Jira REST client, retry và logging.
- Đã pass build/test và verify end-to-end bằng fake Jira REST server local ở phase 11.
- Chưa verify với Jira test instance thật vì chưa có Jira URL/credential thật trong workspace.
- Phase 12 đã bổ sung root README và handoff notes để người khác có thể chạy local và tiếp tục dự án.
