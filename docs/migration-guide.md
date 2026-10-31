# Cập nhật database bằng EF Core migration

Chạy các lệnh dưới đây bằng **CMD**, tại thư mục gốc ShopFlow.

**Chỉ có một database PostgreSQL trong Docker.** Docker Compose khởi động database; EF chạy trên máy Windows (host) kết nối tới database đó qua `localhost:5433` để cập nhật bảng. API bên trong Docker dùng `db:5432`.

## 1. Khởi động database

```cmd
docker compose -f Docker-compose.yaml up -d db
docker compose -f Docker-compose.yaml ps
```

Chờ service `db` báo `healthy`. Bước này chưa cập nhật bảng.

## 2. Đặt kết nối cho cửa sổ CMD hiện tại

```cmd
set "ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=TEN_DB;Username=TEN_USER;Password=MAT_KHAU"
```

Thay các giá trị bằng `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` trong `.env`. Không ghi mật khẩu thật vào tài liệu này. EF chạy trên host không tự đọc `.env` của Compose. Mở cửa sổ CMD mới thì đặt lại biến.

## 3. Tạo migration sau khi sửa entity/mapping

```cmd
dotnet ef migrations add TEN_MIGRATION --project src/ShopFlow.Api
```

Thay `TEN_MIGRATION` bằng tên mô tả thay đổi, ví dụ `AddProducts`. Đọc file mới trong `src/ShopFlow.Api/Migrations`: `Up()` áp dụng thay đổi, `Down()` đảo ngược thay đổi. Kiểm tra trước khi áp dụng, nhất là thao tác xóa bảng/cột.

**Nếu migration đã được tạo thì bỏ qua bước này.** Không tạo lại `InitialCreate`.

## 4. Áp dụng migration vào PostgreSQL trong Docker

```cmd
dotnet ef database update --project src/ShopFlow.Api
```

Chạy trong cùng cửa sổ CMD đã đặt biến kết nối. Kết quả mong đợi: `Done.`; EF chỉ áp dụng các migration chưa chạy.

## 5. Kiểm tra kết quả

```cmd
dotnet ef migrations list --project src/ShopFlow.Api
```

Các migration cần áp dụng không còn ghi `(Pending)`. Có thể mở pgAdmin/DBeaver tại `localhost:5433` để kiểm tra bảng, cột và index.

## Khi cần cập nhật cả API trong Docker

```cmd
docker compose -f Docker-compose.yaml up -d --build api
```

Chạy sau khi cập nhật schema nếu cần đưa code mới vào container API. Lệnh build/restart API không tự chạy migration trong cấu hình hiện tại.

**Lần sau:** sửa entity/mapping → bảo đảm db healthy và có biến kết nối → tạo migration → đọc kiểm tra → database update → kiểm tra kết quả.

Không dùng `docker compose down -v` nếu muốn giữ dữ liệu.
