# ShopFlow

Backend cửa hàng bàn phím cơ lắp sẵn, xây dựng trong quá trình học ASP.NET Core. Mỗi cấu hình bàn phím là một SKU có giá và tồn kho riêng.

## Công nghệ và trạng thái

- ASP.NET Core / .NET 10, kiến trúc Controller → Service → Repository → DbContext.
- Entity Framework Core, Npgsql và PostgreSQL 17.
- JWT với hai quyền Customer và Admin.
- Docker Compose chạy API và database; xUnit cho kiểm thử.

Tại thời điểm 10/11/2026:

| Phần | Trạng thái |
| --- | --- |
| Đăng ký, đăng nhập, JWT và phân quyền | Đã triển khai |
| CRUD danh mục | Đã triển khai; đổi tên danh mục có sản phẩm cần xác nhận |
| Tạo, đọc và cập nhật sản phẩm Admin | Đã triển khai; PUT kiểm tra RowVersion |
| Catalog công khai | Chi tiết, phân trang, tìm kiếm, lọc và sắp xếp |
| Ngừng bán qua DELETE sản phẩm | Chưa triển khai endpoint |
| Giỏ hàng | Có entity Cart cho đăng ký; chưa có API giỏ hàng |
| Checkout, đơn hàng, frontend React, chatbot | Chưa triển khai |

Đây là dự án đang phát triển. Build thành công không đồng nghĩa toàn bộ nghiệp vụ đã được kiểm thử. Bằng chứng và phần còn lại được ghi trong [notes.md](notes.md).

## Yêu cầu

- Git.
- Docker Desktop đang chạy, dùng Linux containers và Docker Compose.
- .NET SDK 10 để chạy EF migration và test trên máy theo hướng dẫn này.
- Postman hoặc công cụ gọi HTTP; DBeaver là tùy chọn.
- Các cổng 8080, 5433 còn trống; cổng 5434 dùng khi chạy integration test.

Dockerfile đã chứa bước build bằng SDK và chạy bằng ASP.NET runtime. Tuy nhiên, quy trình migration hiện tại vẫn chạy EF CLI trên máy, nên hướng dẫn này chưa phải quy trình chỉ cần Docker.

**Các lệnh bên dưới dùng PowerShell**, chạy từ thư mục gốc repository. Hướng dẫn migration bằng CMD nằm tại [docs/migration-guide.md](docs/migration-guide.md).

## Chạy lần đầu sau khi clone

### 1. Lấy mã nguồn

Thay `URL_REPOSITORY` bằng địa chỉ Git của dự án:

```powershell
git clone URL_REPOSITORY ShopFlow
cd ShopFlow
```

Git chỉ chuyển các file đã commit. Database trong volume, container, image cục bộ và file `.env` của người khác không đi kèm khi clone.

### 2. Tạo cấu hình riêng

Tạo file `.env` cạnh `Docker-compose.yaml` với nội dung sau, thay hai giá trị `THAY_...` trước khi chạy:

```dotenv
POSTGRES_DB=shopflow
POSTGRES_USER=shopflow
POSTGRES_PASSWORD=THAY_BANG_MAT_KHAU_RIENG
JWT_KEY=THAY_BANG_KEY_BASE64
```

Tạo JWT key ngẫu nhiên gồm 32 byte bằng PowerShell, rồi chép kết quả vào `JWT_KEY` trên máy của bạn:

```powershell
$jwtBytes = New-Object byte[] 32
$jwtRng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtRng.GetBytes($jwtBytes)
$jwtRng.Dispose()
[Convert]::ToBase64String($jwtBytes)
```

Key phải là Base64 hợp lệ, giải mã được ít nhất 32 byte. Không dùng placeholder làm key. `.env` đã được Git bỏ qua; không commit mật khẩu, key hoặc token vào code, comment hay tài liệu.

### 3. Khởi động PostgreSQL

```powershell
docker compose -f Docker-compose.yaml up -d db
docker compose -f Docker-compose.yaml ps
```

Chờ `db` ở trạng thái `healthy`. Bước này tạo database nhưng chưa tạo các bảng ứng dụng.

| Nơi kết nối | Host | Port |
| --- | --- | --- |
| EF CLI, DBeaver, API chạy trên máy | localhost | 5433 |
| API trong Docker Compose | db | 5432 |
| Integration test chạy trên máy | localhost | 5434 |

### 4. Áp dụng migration đã có

```powershell
dotnet restore ShopFlow.slnx
dotnet tool list --global
```

Nếu chưa có `dotnet-ef`, cài công cụ EF CLI nhánh 10.x. Công cụ này dùng để áp dụng các migration từ mã nguồn vào database:

```powershell
dotnet tool install --global dotnet-ef --version "10.*"
```

Nếu đã có công cụ, kiểm tra `dotnet ef --version`; chỉ khi cần chuyển sang nhánh 10.x mới chạy `dotnet tool update --global dotnet-ef --version "10.*"`.

Đặt connection string trong **cùng cửa sổ PowerShell sẽ chạy EF**. Thay `TEN_DB`, `TEN_USER`, `MAT_KHAU` bằng các giá trị tương ứng trong `.env`:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5433;Database="TEN_DB";Username="TEN_USER";Password="MAT_KHAU"'
dotnet ef database update --project src/ShopFlow.Api
dotnet ef migrations list --project src/ShopFlow.Api
```

EF CLI không tự đọc `.env` của Compose. Mở terminal mới thì cần đặt lại biến. Nếu mật khẩu có ký tự đặc biệt, cần giữ đúng cú pháp chuỗi PowerShell và connection string Npgsql.

Người clone dự án chỉ cần **áp dụng migration đã có**, không tạo lại `InitialCreate` hay migration mới. API hiện không tự migrate khi khởi động.

### 5. Build API và seed dữ liệu mẫu

```powershell
docker compose -f Docker-compose.yaml build api
docker compose -f Docker-compose.yaml run --rm api --seed
docker compose -f Docker-compose.yaml up -d api
docker compose -f Docker-compose.yaml ps
```

`--seed` tạo danh mục `75%` và sản phẩm `DEMO75-BLK-LIN` nếu chưa tồn tại. Seed không ghi đè giá/tồn kho của SKU đã có và không tạo tài khoản Admin. Lệnh seed kết thúc sau khi xử lý dữ liệu; lệnh `up` tiếp theo mới chạy API phục vụ HTTP.

Kiểm tra kết nối database:

```powershell
Invoke-RestMethod http://localhost:8080/health/db
```

Mong đợi `status: healthy`. Health check chỉ kiểm tra kết nối, không chứng minh migration hoặc nghiệp vụ đã đúng. Thử tiếp `GET http://localhost:8080/api/products` để đọc catalog.

## Tài khoản và thử API

Base URL: `http://localhost:8080`. Body JSON dùng `Content-Type: application/json`.

Đăng ký bằng `POST /api/auth/register` với `email`, `password`, `fullName`. Tài khoản đăng ký luôn là Customer. Đăng nhập bằng `POST /api/auth/login` với `email`, `password`, rồi lấy `accessToken` từ response.

Trong Postman chọn **Authorization → Bearer Token** cho các endpoint cần đăng nhập. Token hết hạn thì đăng nhập lại; thời hạn hiện cấu hình là 60 phút.

### Tạo quyền Admin cho demo local

Hiện chưa có seed Admin tự động hoặc API cấp quyền. Để thử chức năng Admin trên database local do bạn quản lý:

1. Đăng ký một tài khoản demo qua API.
2. Dùng DBeaver kết nối database tại `localhost:5433` bằng cấu hình riêng trong `.env`.
3. Chạy SQL dưới đây sau khi thay email bằng đúng tài khoản demo cần cấp quyền:

```sql
UPDATE "Users"
SET "Role" = 'Admin'
WHERE "Email" = 'THAY_BANG_EMAIL_DEMO';
```

Kiểm tra chỉ một dòng được cập nhật, commit nếu công cụ đang dùng transaction thủ công, rồi **đăng nhập lại** để nhận token mang role Admin. Token Customer đã cấp trước đó không tự đổi quyền. Không dùng thao tác cấp quyền thủ công này làm quy trình quản trị cho môi trường triển khai thật.

### Các endpoint hiện có

| Method | Đường dẫn | Quyền |
| --- | --- | --- |
| GET | `/health/db` | Public |
| POST | `/api/auth/register`, `/api/auth/login` | Public |
| GET | `/api/auth/me` | Đã đăng nhập |
| GET | `/api/auth/admin-check` | Admin |
| GET | `/api/categories` | Public |
| GET | `/api/products`, `/api/products/{id}` | Public |
| GET, POST | `/api/admin/categories` | Admin |
| GET, PUT, DELETE | `/api/admin/categories/{id}` | Admin |
| GET, POST | `/api/admin/products` | Admin |
| GET, PUT | `/api/admin/products/{id}` | Admin |

Danh sách sản phẩm public nhận `q`, `categoryId`, `minPrice`, `maxPrice`, `switchType`, `connectivity`, `sort`, `page`, `size`. Ví dụ:

```http
GET /api/products?switchType=Linear&connectivity=Wired&maxPrice=2000000&sort=priceAsc&page=1&size=20
```

- Mặc định `page=1`, `size=20`, `sort=newest`; size tối đa 100.
- Sort hỗ trợ `newest`, `priceAsc`, `priceDesc`, `nameAsc`, có Id làm khóa phụ.
- `q` tìm phần chuỗi trong Name hoặc Sku; với cấu hình PostgreSQL mặc định hiện tại, phân biệt hoa/thường và không có xử lý bỏ dấu.
- Connectivity lọc một giá trị `Wired`, `Bluetooth` hoặc `Wireless2_4G`; sản phẩm hỗ trợ nhiều kết nối vẫn khớp nếu chứa kết nối được chọn.
- Chỉ hiển thị sản phẩm active. Sản phẩm active hết hàng vẫn xuất hiện; chi tiết inactive trả 404.

### PUT sản phẩm và RowVersion

GET chi tiết bằng endpoint **Admin** để lấy RowVersion, sau đó PUT cùng ID với đầy đủ các trường cập nhật:

```json
{
  "categoryId": 1,
  "sku": "DEMO75-BLK-LIN",
  "name": "Bàn phím Demo 75",
  "brand": "Demo",
  "switchType": "Linear",
  "connectivity": ["Wired"],
  "color": "Black",
  "isHotSwappable": true,
  "description": "Dữ liệu demo",
  "price": 1600000,
  "stock": 20,
  "isActive": true,
  "rowVersion": 123
}
```

Thay ID danh mục, SKU và `123` bằng dữ liệu thực tế. `stock=20` đặt số tồn thành 20, không cộng thêm 20. Thành công trả 200 và RowVersion mới; gửi phiên bản cũ phải trả 409 với `CONCURRENCY_CONFLICT`. Thao tác PUT không thay đổi nội dung vẫn thực hiện kiểm tra phiên bản khi lưu.

Quy tắc khóa cấu hình sản phẩm sau khi có OrderItem trong spec chưa được triển khai vì schema đơn hàng chưa có.

## Build và kiểm thử

Build source trên máy:

```powershell
dotnet build ShopFlow.slnx
```

Chạy các test không thuộc lớp integration đăng ký, không cần khởi động database test:

```powershell
dotnet test ShopFlow.slnx --filter "FullyQualifiedName!~RegisterIntegrationTests"
```

Chạy toàn bộ test, gồm integration dùng PostgreSQL riêng:

```powershell
docker compose -f Docker-compose.yaml up -d db-test
docker compose -f Docker-compose.yaml ps db-test
dotnet test ShopFlow.slnx
```

Chờ `db-test` healthy trước khi test. Integration test hiện dùng database `shopflow_test` tại `localhost:5434`, tự áp dụng migration cho database test và tạo/dọn dữ liệu thử. Đây là service riêng với thông tin đăng nhập chỉ dành cho test local trong Compose; không đổi test sang database chứa dữ liệu thật. Service `db-test` không có named volume như `db`.

Bộ test hiện có tập trung vào auth, đăng ký và cập nhật danh mục. Chưa có bộ kiểm thử tự động đầy đủ cho filter/sort hoặc PUT sản phẩm. Các xác nhận Postman được ghi riêng trong notes, không thay thế kết quả test tự động.

## Khi pull code mới

```powershell
git pull
```

Nếu có migration mới, đặt lại biến kết nối trong terminal và chạy `dotnet ef database update --project src/ShopFlow.Api`. Sau đó build lại API:

```powershell
docker compose -f Docker-compose.yaml up -d --build api
```

Pull source không tự cập nhật image/container đang chạy. Không cần seed lại trừ khi muốn bổ sung dữ liệu mẫu còn thiếu.

## Vận hành local và lỗi thường gặp

```powershell
docker compose -f Docker-compose.yaml logs --tail 100 api
docker compose -f Docker-compose.yaml logs --tail 100 db
docker compose -f Docker-compose.yaml stop
```

Database chính lưu trong named volume `postgres-data` của Compose. `stop` hoặc `down` không kèm `-v` giữ volume; **không dùng `down -v` khi cần giữ dữ liệu**. Đổi `POSTGRES_PASSWORD` trong `.env` không tự đổi mật khẩu của database đã khởi tạo trong volume.

| Triệu chứng | Kiểm tra |
| --- | --- |
| Compose báo thiếu biến | File `.env` phải nằm cạnh Compose và có đủ bốn biến |
| JWT key lỗi | Key phải Base64, giải mã được ít nhất 32 byte |
| EF không kết nối được DB | `db` healthy; host localhost, port 5433; biến kết nối đặt trong chính terminal chạy EF |
| API báo thiếu bảng | Áp dụng migration; khởi động Compose không tự tạo schema ứng dụng |
| API vẫn chạy code cũ | Chạy lại `up -d --build api` |
| 401 | Token thiếu, sai hoặc hết hạn; đăng nhập lại |
| 403 | Đã xác thực nhưng thiếu quyền Admin |
| PUT trả 409 | Đọc mã lỗi: SKU trùng hoặc RowVersion cũ; GET lại trước khi sửa tiếp |
| Truy cập `/` hoặc `/swagger` không có trang | Chưa có frontend hoặc Swagger UI; gọi endpoint API bằng Postman |

OpenAPI JSON chỉ được map trong môi trường Development tại `/openapi/v1.json`. Compose hiện không bật Development, nên endpoint này không có mặc định.

Trước khi chia sẻ repository, kiểm tra không còn credential thật trong comment hoặc file tracked; `.gitignore` không bảo vệ secret đã nằm trong file được commit. Nếu secret đã lộ qua Git, cần đổi secret đó.

## Cấu trúc và tài liệu

```text
src/ShopFlow.Api/
  Controllers/    HTTP endpoints và quyền truy cập
  Dtos/           Request/response
  Entities/       Entity và enum
  Services/       Luồng nghiệp vụ
  Repositories/   Truy cập dữ liệu
  Data/           DbContext và seed
  Migrations/     Lịch sử schema
tests/ShopFlow.Tests/
docs/
Dockerfile
Docker-compose.yaml
```

- [Spec nghiệp vụ](docs/superpowers/specs/2026-10-30-shopflow-design.md): thiết kế mục tiêu; không phải mọi phần đã triển khai.
- [Tiến độ và bằng chứng](notes.md).
- [Kế hoạch học](docs/learning-plan.json).
- [Migration bằng CMD](docs/migration-guide.md).
- [Checklist Docker dùng chung](docs/docker-checklist.md).

PersonalProgress là ứng dụng theo dõi học tập riêng, không cần để chạy ShopFlow và không thuộc Compose này.
