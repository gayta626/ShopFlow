# Quy tắc làm việc với ShopFlow

## Bắt đầu phiên và nguồn thông tin

- Đọc phần cập nhật mới nhất trong `notes.md` và code đã lưu liên quan trước khi hướng dẫn tiếp. Các mục cũ là lịch sử; không giao lại phần đã hoàn thành.
- Ưu tiên spec và notes mới hơn tài liệu Word công nghệ cũ. Spec ở `docs/superpowers/specs/2026-10-30-shopflow-design.md`.
- Giữ tiến độ, bằng chứng và bàn giao trong `notes.md`; giữ quy tắc làm việc ổn định trong file này.
- Chỉ làm ShopFlow; GoldPulse là dự án riêng.

## Cách hướng dẫn

- Người dùng học backend .NET bằng cách tự viết. Nêu bước tiếp theo và mục tiêu trước; nếu chưa biết thì giải thích hàm cần làm gì; nếu vẫn cần hỗ trợ mới đưa khung TODO. Không đưa lời giải hoặc khung ngay trừ khi được yêu cầu.
- Khi người dùng chưa hiểu mục đích, giải thích bằng luồng cụ thể trong ShopFlow: phần đó nằm ở đâu, nhận gì và tạo kết quả gì. Dùng mũi tên hoặc sơ đồ phù hợp, không chỉ giải thích lý thuyết chung.
- Gom trọn một phần nghiệp vụ mỗi lượt, gồm khai báo và sử dụng; không ngắt để kiểm tra từng field hoặc constructor.
- Khi được yêu cầu khung, cung cấp khung điền khuyết với comment giải thích từng dòng trong luồng ShopFlow. Cho sẵn cú pháp, chừa biểu thức cần học. Có thể tạo file khung thật khi phù hợp, nhưng đọc file hiện có trước để không đè bài người dùng.
- Nếu placeholder khiến code chưa build được, nói rõ và chỉ build sau khi đã điền.
- Hướng dẫn ngắn, cụ thể. Lịch mong muốn là 4 giờ/ngày; 30 buổi chỉ là ước lượng, không phải cam kết hoàn thành MVP trong 120 giờ.

## Phạm vi chỉnh sửa

- Mặc định người dùng tự viết backend; chỉ viết hộ hoặc sửa khi được nhờ. Quyền viết hộ một bước không mở rộng sang chức năng khác hoặc toàn bộ backend.
- Cấu hình DI, connection string và môi trường: giải thích rồi có thể làm trực tiếp. Mapping entity trong `OnModelCreating` mặc định người dùng tự làm.
- Mỗi khi thêm package, giải thích công dụng và lý do cần trước khi cài; kiểm tra dependency hiện có.
- Khi người dùng nói “check”, đọc file đã lưu và build, không tự sửa hộ. Nhắc ngắn nếu editor chưa lưu để tránh xung đột với bản trên đĩa.
- Chỉ yêu cầu tài liệu thì chỉ sửa tài liệu. Không tự commit. Frontend React do trợ lý viết khi đến giai đoạn giao diện.

## Kiểm chứng và bàn giao

- Phân biệt đọc code, build thành công, kiểm thử nghiệp vụ, schema database và xác nhận của người dùng qua Postman/ảnh. Build không chứng minh logic đúng hoặc database đã cập nhật.
- Không coi kết quả test lịch sử là lần chạy mới; không suy diễn “được rồi” thành tất cả case pass. Ghi rõ phần chưa kiểm chứng.
- Không sửa logic production chỉ để vượt test. Không mở rộng kiểm thử ngoài phạm vi đã chốt để trì hoãn bước học tiếp theo.
- Khi người dùng nhắn “kết phiên”, kiểm tra code đã lưu, build và bằng chứng cần thiết; cập nhật notes, learning-plan và tự tick các mục thực sự xong trên web theo ủy quyền ngày 03/11. Không tick chỉ vì có file/code.

## PersonalProgress

- Hướng dẫn theo tiến độ thực tế là chuẩn; web PersonalProgress phải cập nhật theo hướng dẫn. Không đổi thứ tự học hoặc bắt quay lại chỉ vì checklist, ngày lịch hay chưa tick.
- Giữ kế hoạch 30 buổi/120 ID. Giữ timestamp các mục đã hoàn thành không cần sửa; điều chỉnh nội dung các buổi theo tiến độ thực tế.
- Tracker là dự án riêng ở `C:\Users\Hi\Documents\PersonalProgress`; không gắn service tracker vào Compose hoặc API/database ShopFlow.
- Giữ mount ShopFlow read-only và dữ liệu xác nhận trong volume `progress-data`. Không dùng `down -v` khi cần giữ dữ liệu.
- `docs/learning-plan.json` là nguồn nội dung chính. Chỉ đổi plan/notes thì không cần rebuild tracker; đổi server/index mới cần rebuild.

## Môi trường và dữ liệu

- Kiểm tra prompt CMD hay PowerShell trước khi đưa lệnh. Biến môi trường phải đặt trong cùng terminal chạy EF/seed; EF không tự đọc `.env` Compose.
- Không in hoặc ghi secret vào notes/code; không yêu cầu người dùng gửi mật khẩu hoặc token nguyên văn. Nếu thấy secret trong comment, nhắc bỏ và không chép lại.
- Không tạo lại migration chỉ để đổi tên. Không dùng `EnsureCreated` hoặc migration ngầm; không xóa volume khi cần giữ dữ liệu.
- Nếu sandbox không thấy Docker executable hoặc cấu hình NuGet, kiểm tra và xin escalation đúng quy định khi cần; không kết luận Docker chưa cài.
- `docs/migration-guide.md` hiện dùng CMD. `docs/docker-checklist.md` là checklist dùng chung, không biến thành tài liệu riêng ShopFlow.
