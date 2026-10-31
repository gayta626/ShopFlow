# ShopFlow — Tiến độ và bàn giao

Cập nhật: 31/10/2026 (Asia/Saigon), bàn giao phiên học EF Core và tạo công cụ tiến độ cá nhân.

## Tiếp tục từ đây

- User hoàn thiện: đã sửa CreatedAt khởi tạo UTC, bỏ max length của timestamp, thêm NormalizedEmail required/max254. Đã đọc CreateUsersTable: cột/độ dài/NOT NULL, unique NormalizedEmail và check Role đúng; build 0 warning/0 error. Bước tiếp theo: người dùng áp dụng database update và kiểm tra migration/bảng Users. Chưa xác nhận migration Users đã áp dụng.

- User: đã có User/UserRole/DbSet và mapping, build 0 warning/0 error. Kiểm tra mới nhất còn 3 điểm cần người dùng sửa: CreatedAt chưa khởi tạo UtcNow; mapping CreatedAt còn HasMaxLength(20) cần bỏ; NormalizedEmail thiếu mapping max 254 (unique index đã có). Tên NormalizedEmail/CreatedAt, bảng Users, PasswordHash 512, FullName 100, Role conversion/string max20 và check Customer/Admin đã đúng. Role là enum không nullable nên required theo convention. Chưa tạo migration Users; hoàn thiện ba điểm rồi tạo CreateUsersTable và kiểm tra trước áp dụng.

- Theo yêu cầu, đã đổi DbSeeder.SeedAsync thành Task<int>, trả kết quả SaveChangesAsync sau cả hai nhánh kiểm tra; bỏ return sớm khi SKU tồn tại để Category mới vẫn được lưu nếu có. Program.cs in không có thay đổi khi kết quả 0, hoặc số bản ghi mới khi >0 (seed hiện chỉ thêm). Build 0 warning/0 error; chưa chạy lại seed để xác minh console sau sửa đổi này.

- Đã truy vấn trực tiếp PostgreSQL trong Compose: Categories có đúng 1 dòng Id=1, Name/NormalizedName="75%"; SKU DEMO75-BLK-LIN có đúng 1 dòng Id=1, CategoryId=1, join đúng CategoryName="75%". Đã xác minh seed lưu cả hai bảng. Chưa kiểm chứng riêng việc chạy seed lại giữ nguyên giá/tồn đã sửa; bước tiếp theo là kiểm tra tính chạy lại an toàn trước khi kết thúc seed và chuyển User/auth. Không tự đánh dấu toàn bộ kiểm chứng catalog hoàn thành.

- Mốc seed mới nhất: đã kiểm tra Program.cs có nhánh args.Contains("--seed"), tạo DI scope/lấy ShopFlowDbContext, await SeedAsync rồi return; đúng. Build toàn bộ 0 warning/0 error. Bước tiếp theo là người dùng chạy `dotnet run --project src/ShopFlow.Api --no-launch-profile -- --seed` hai lần trong terminal có connection string và kiểm tra Category/SKU trong database; chưa có kết quả chạy seed.

- Seed: người dùng đã điền xong DbSeeder.SeedAsync; đọc lại xác nhận tìm Category theo NormalizedName, chỉ thêm khi thiếu, tìm Product theo Sku và chỉ thêm khi thiếu, gắn Category navigation rồi SaveChangesAsync. Build toàn bộ thành công 0 warning/0 error. Chưa nối/chạy seed hoặc kiểm chứng database hai lần. Bước tiếp theo: thêm chế độ chạy seed chủ động trong Program.cs (ví dụ --seed), tạo DI scope lấy DbContext, gọi seed rồi thoát; sau đó chạy hai lần kiểm tra không nhân bản. Comment gọi service ở dòng NormalizedName là ghi chú cũ cần bỏ, không phải logic đã triển khai.

- Mốc mới nhất: đã đọc `20261031102053_AddProducts.cs`, xác nhận bảng/cột/check/index SKU unique và FK CategoryId với Restrict đúng. Người dùng báo đã áp dụng xong sau khi đổi cú pháp connection string từ CMD sang PowerShell; chưa có output database để kiểm chứng độc lập. Bước kế tiếp: hướng dẫn tự viết `Data/DbSeeder.cs`, seed demo Category/SKU theo khóa NormalizedName/Sku, chạy lại không nhân bản hoặc ghi đè tồn/giá. Chưa viết/chạy seed; không tự tick web.

- Mốc mới nhất: đã kiểm tra Product.Category = null!, quan hệ HasOne/WithMany/HasForeignKey(CategoryId)/OnDelete(Restrict) đúng, bỏ HasMany thừa ở Category. Build toàn bộ bằng `dotnet build ShopFlow.slnx --no-restore --no-incremental`: 0 warning/0 error. Hiện chỉ có InitialCreate; bước kế tiếp là người dùng tạo migration AddProducts, đọc FK/check/index rồi áp dụng và kiểm tra. TODO cũ trong Product chỉ là comment còn sót, không ảnh hưởng chức năng.

- Product: đã có entity, SwitchType/Connectivity enum, CategoryId/navigation, Category.Products và DbSet Products. Theo yêu cầu, trợ lý đã hoàn thiện mapping Product (độ dài, enum, precision, checks, indexes, xmin) nhưng chừa TODO quan hệ/FK với DeleteBehavior.Restrict cho người dùng tự làm. Chưa tạo migration Product. Build thành công với 1 warning CS8618: Product.Category chưa khởi tạo null!. EF có thể suy luận quan hệ theo convention; cần hoàn thiện Restrict trước migration để tránh cascade xóa sản phẩm.

- Người dùng báo đã xong sau hướng dẫn đặt connection string trong CMD và áp dụng migration; chưa cung cấp output `Done.` hoặc kết quả kiểm tra bảng để xác minh độc lập. Chuyển sang hướng dẫn nhóm việc tạo Product, enum, navigation Category–Product và DbSet; chưa triển khai các file này thay người dùng.

- Tiến độ mới nhất: người dùng đã tạo `20261031041454_InitialCreate`. Trợ lý đã đọc migration: Categories có Id identity/PK, Name và NormalizedName varchar(100) NOT NULL, unique index trên NormalizedName; Down xóa bảng. Chưa xác nhận áp dụng database. Shell trợ lý không tìm thấy lệnh docker; hướng dẫn người dùng kiểm tra Compose và chạy database update từ máy với localhost:5433.

- Chuẩn bị migration: đã xác nhận `dotnet ef` 10.0.14 có sẵn; đã thêm `Microsoft.EntityFrameworkCore.Design` 10.0.4 tương ứng EF Core đang restore. Chưa chạy tạo hoặc áp dụng migration. Người dùng sẽ thực hành `dotnet ef migrations add InitialCreate --project src/ShopFlow.Api` từ gốc repo, rồi đọc Up/Down và snapshot.

**Đã hoàn thiện Category với `NormalizedName` và mapping required/max 100/unique index. Build thành công, 0 warning/0 error. Bước tiếp theo là migration; chưa tạo migration hoặc bảng nghiệp vụ.**

- `src/ShopFlow.Api/Data/ShopFlowDbContext.cs`: kế thừa DbContext, constructor nhận options và gọi base(options), có `DbSet<Category> Categories` nằm ngoài constructor.
- `Program.cs`: đã đăng ký `AddDbContext<ShopFlowDbContext>` + `UseNpgsql`, đọc connection string `DefaultConnection`.
- `Docker-compose.yaml`: service api có `ConnectionStrings__DefaultConnection`, Host=db, Port=5432, lấy Database/Username/Password từ các biến POSTGRES_* trong `.env`. Không cần biến DefaultConnection riêng trong `.env`.
- `Controllers/HealthController.cs`: route `[Route("health")]` và `[HttpGet("db")]` tạo `GET /health/db`; `CanConnectAsync` trả 200/healthy hoặc 503/unhealthy. Người dùng báo test được, không cung cấp response để trợ lý xác minh độc lập. Đây chỉ là kiểm tra kết nối, không chứng minh schema đúng.
- `Entities/Category.cs`: có `int Id`, `string Name = string.Empty` và `string NormalizedName = string.Empty`. Còn hai using DataAnnotations/Schema không dùng, không phải lỗi chức năng.
- Mapping Category trong `OnModelCreating`: bảng Categories, khóa chính Id, Name và NormalizedName required/max 100; NormalizedName có unique index. `IsRequired` không chặn chuỗi rỗng; trim/độ dài tối thiểu làm ở phần nghiệp vụ.
- Trợ lý đã bổ sung mapping NormalizedName theo yêu cầu trực tiếp của người dùng, chạy `dotnet build ShopFlow.slnx --no-restore`: thành công, 0 warning/0 error. Đây là hỗ trợ trong phạm vi bước này, không thay đổi mặc định người dùng tự viết mapping.

**Bước học tiếp theo:** hướng dẫn EF tooling/design-time, tạo và đọc migration rồi áp dụng, kiểm tra bảng/constraint. Không tạo lại DbContext/connection string hoặc mapping Category đã xong. Logic chuẩn hóa tên khi tạo/sửa chưa triển khai.

Phiên này tạm chuyển sang làm công cụ theo dõi cá nhân **PersonalProgress**, nằm ngoài ShopFlow; chi tiết và cách chạy ở mục riêng bên dưới.

## Cách làm việc đã thống nhất

- Cách hướng dẫn mới nhất: đầu tiên chỉ nêu bước tiếp theo và mục tiêu để người dùng tự thử; nếu chưa biết, giải thích hàm cần làm những công việc gì; chỉ khi vẫn chưa biết/cần hỗ trợ thêm mới đưa khung TODO. Không đưa khung hoặc lời giải ngay. Vẫn gom việc liên quan thành bước hợp lý; yêu cầu viết hộ cụ thể chỉ áp dụng trong phạm vi được nhờ.

- Quy tắc ưu tiên do người dùng làm rõ: **lộ trình hướng dẫn của trợ lý theo tiến độ code thực tế là chuẩn; web PersonalProgress chỉ theo dõi lại lộ trình đó**. Không lấy lịch/checklist web để đổi thứ tự hướng dẫn, bắt quay lại học mục cũ hoặc chặn chuyển bước vì chưa bấm xác nhận. Khi hướng dẫn thay đổi, cập nhật `docs/learning-plan.json` để web phản ánh đúng; không dùng file này làm nguồn quyết định thứ tự học. Web đọc lại mỗi 2 giây. Giữ ID và xác nhận cũ khi nội dung còn tương ứng; không suy ra đã kiểm chứng chỉ từ việc có code. Lộ trình đang đi: Category → InitialCreate → Product/enum/navigation → mapping/FK → AddProducts → seed → các chức năng tiếp theo. Triển khai Trim/ToUpperInvariant khi viết tạo/sửa Category, không giao bài tập riêng hoặc buộc theo ngày 10 trên web.

- Mỗi khi thêm package, giải thích trước package dùng để làm gì và vì sao cần cho bước hiện tại; không chỉ báo tên/phiên bản hoặc chạy lệnh cài.

- Làm rõ mới nhất: người dùng tự viết cấu hình thuộc tính/mapping entity trong `OnModelCreating` (bao gồm required, max length, unique index). Trợ lý hướng dẫn và kiểm tra, không tự sửa phần này nếu chưa được nhờ. Ngoại lệ cấu hình được làm trực tiếp vẫn áp dụng cho DI, connection string và môi trường.

- Cập nhật mới nhất: gom các bước liên quan có thể làm ngay thành một lượt học hợp lý, không tách quá nhỏ (ví dụ NormalizedName gồm thêm thuộc tính, giải thích chuẩn hóa, cấu hình ràng buộc/unique index và build). Vẫn để người dùng tự thực hành phần code nghiệp vụ; trợ lý thực hiện phần cấu hình. Hướng dẫn theo một nhóm việc có kết quả rõ ràng, thay cho yêu cầu xác nhận sau từng thay đổi nhỏ.

- Cập nhật sở thích: với các bước cấu hình (DI, connection string, cấu hình môi trường...), trợ lý giải thích rồi trực tiếp thực hiện, không cần chờ người dùng tự viết. Phần học code/nghiệp vụ vẫn hướng dẫn để người dùng tự thực hành, trừ khi được nhờ làm.

- Người dùng chủ yếu học backend: **chỉ hướng dẫn từng bước, giải thích cú pháp; không tự viết/sửa backend hoặc Docker thay người dùng nếu chưa được nhờ**.
- Trợ lý phụ trách viết frontend React là chính khi đến phần giao diện. Chưa bắt đầu frontend.
- Mỗi bước nên có vị trí file, lệnh chạy, ý nghĩa phần cần thay và kết quả mong đợi; tránh dồn quá nhiều nội dung.
- Cách hướng dẫn từ nay: khi bắt đầu mỗi bước, chỉ nói trước bước tiếp theo cần làm gì và mục tiêu của bước đó, để người dùng tự thử. Nếu người dùng chưa biết cách làm hoặc yêu cầu hỗ trợ, mới giải thích và đưa khung code có chỗ trống/TODO để người dùng tự điền; không đưa ngay lời giải/code hoàn chỉnh.
- Ngoại lệ đã được người dùng yêu cầu: phần cấu hình thì giải thích rồi làm luôn; nếu người dùng yêu cầu điền/viết hộ một bước thì làm trong phạm vi bước đó. Không suy ra quyền tự triển khai toàn bộ backend.
- Người dùng cần biết rõ đang làm bước nào, ở file nào, để làm gì; từng bị mất mạch khi trợ lý đi nhanh từ DI sang health endpoint. Giữ mỗi lượt học nhỏ và giải thích trước khi thao tác.
- Lịch học mong muốn: **4 giờ/ngày**, chia đầu việc cụ thể và tiêu chí hoàn thành theo từng ngày; không chỉ checklist tổng quát.
- Với Docker, phân biệt rõ image, container name/ID, service name, cổng máy/cổng container.
- Lưu quyết định, sở thích và tiến độ vào notes để phiên khác tiếp tục. Khi chỉ yêu cầu tài liệu, chỉ cập nhật tài liệu.
- Người dùng từng muốn dùng Superpowers; không tự coi đây là yêu cầu tạo agent hoặc triển khai toàn bộ sản phẩm. Phiên này làm theo hình thức hướng dẫn người dùng tự thao tác.

## Quyết định kỹ thuật và phạm vi

- Dự án cửa hàng **bàn phím cơ lắp sẵn**, mỗi cấu hình là một SKU có kho riêng. Chỉ làm ShopFlow; GoldPulse là dự án riêng.
- Backend: .NET 10, Controller–Service–Repository trong một API project, EF Core + **PostgreSQL qua Npgsql**. PostgreSQL thay lựa chọn SQL Server trước đây.
- Frontend: **React**, thay HTML/CSS/JS thuần; mã nguồn dự kiến ở `web/`. Chưa chốt tooling hoặc thư viện giao diện.
- Hướng đóng gói hiện tại: build React thành file tĩnh đưa vào `wwwroot`, phục vụ cùng container ASP.NET Core; database container riêng. Chưa có yêu cầu tách FE bằng Nginx. Dev server FE có thể chạy riêng khi phát triển.
- MVP gồm mua hàng, quản trị, demo web và chatbot tư vấn catalog. Reviews/báo cáo để sau.
- 7 bảng dự kiến: Users, Categories, Products, Carts, CartItems, Orders, OrderItems.
- JWT Admin/Customer; Customer chỉ hủy Pending, Admin hủy Pending/Paid; thanh toán mô phỏng; VND, chưa phí vận chuyển.
- Checkout/hủy phải bảo toàn transaction, không bán vượt tồn hoặc hoàn kho hai lần.
- Spec đã chuyển RowVersion sang thuộc tính uint ánh xạ PostgreSQL `xmin` qua Npgsql; Cart.Revision vẫn là concurrency token. Đây là thiết kế, chưa có implementation/test chứng minh.
- Chatbot chỉ đọc catalog/chính sách, không sửa kho/đặt/hủy đơn hoặc đọc dữ liệu riêng. Provider/model/SDK AI chưa chọn.

Spec chi tiết: [2026-10-30-shopflow-design.md](docs/superpowers/specs/2026-10-30-shopflow-design.md). Đã cập nhật React/PostgreSQL, nhưng chưa có implementation plan chi tiết cho toàn bộ MVP. Khi triển khai từng phần phải kiểm tra lại mapping và tính nhất quán của spec theo PostgreSQL.

## Đã hoàn thành / bằng chứng

| Hạng mục | Trạng thái và căn cứ |
|---|---|
| Solution | Có `ShopFlow.slnx`, API và xUnit cùng net10.0 |
| API mẫu | Controllers, OpenAPI Development, WeatherForecast; người dùng xác nhận thấy JSON từ container |
| Kiểm tra môi trường ban đầu | Người dùng báo các bước kiểm tra .NET/Docker/build/test không lỗi; trợ lý chưa xem log build/test đầy đủ |
| Dockerfile | Đã đọc file: SDK 10 build/publish → ASP.NET 10 runtime, cổng 8080, USER APP_UID, chạy ShopFlow.Api.dll |
| `.dockerignore` | Đã có file trong repo |
| Docker Compose | Đã đọc `Docker-compose.yaml`: service api/db, PostgreSQL 17, volume, healthcheck và depends_on service_healthy |
| Khởi động Compose | Ảnh cho thấy build thành công nhưng ban đầu không bind được host port 5432; sau khi đổi 5433, người dùng xác nhận xong |
| EF provider | csproj có `Npgsql.EntityFrameworkCore.PostgreSQL` Version="10.*"; build/restore đã thành công, chưa chốt patch cụ thể |
| EF Core và Category | DbContext, DI, connection string, Category/DbSet/mapping cơ bản; build 0 warning/0 error |
| Health database | Có GET /health/db; người dùng báo đã test được; trợ lý chưa tự gọi endpoint trên container |
| Tài liệu | Đã có spec, notes và checklist Docker dùng chung |

## Cấu hình Docker cần nhớ

- File Compose chính xác: **`Docker-compose.yaml`** (D hoa); dùng `-f` chỉ rõ file.
- Image API: `shopflow-api:dev`. Container chạy thử trước Compose đặt tên `shopflow-api-dev`; đã hướng dẫn dừng để tránh chiếm cổng. Chưa xác nhận đã xóa container cũ.
- API: host 8080 → container 8080. URL mẫu: `http://localhost:8080/WeatherForecast`.
- Database image: `postgres:17`; service name: `db`.
- Database port mapping: **`127.0.0.1:5433:5432`**.
- Named volume trong YAML: `postgres-data`, gắn `/var/lib/postgresql/data`.
- Healthcheck: `pg_isready`; restart policy: `unless-stopped`.
- `.env` dùng POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB. `.gitignore` có `.env`; không ghi secret vào notes hoặc commit. Không đọc/in secret chỉ để kiểm tra tiến độ.
- Service `api` đã có environment `ConnectionStrings__DefaultConnection`; `Program.cs` đã đăng ký DbContext sử dụng cấu hình này.

| Nơi kết nối PostgreSQL | Host | Port |
|---|---|---|
| Máy người dùng: pgAdmin/DBeaver/dotnet run | localhost | 5433 |
| API trong Compose | db | 5432 |

Các lệnh từ gốc repo:

```powershell
docker compose -f Docker-compose.yaml config --quiet
docker compose -f Docker-compose.yaml up -d --build
docker compose -f Docker-compose.yaml ps
docker compose -f Docker-compose.yaml logs --tail 100 db
docker compose -f Docker-compose.yaml logs --tail 100 api
```

Không dùng `down -v` nếu cần giữ dữ liệu. Thay POSTGRES_PASSWORD trong .env không tự đổi mật khẩu database đã khởi tạo.

## Công việc chưa làm

- Các entity/mapping còn lại, migration, seed và schema nghiệp vụ; Category.NormalizedName/unique index đã hoàn thiện.
- Auth/JWT, catalog, cart, checkout, đơn hàng, hoàn kho, xử lý lỗi/rate limit.
- Frontend React và chatbot.
- Swagger UI có Authorize (health database đã có).
- Unit/integration tests nghiệp vụ; test hiện tại vẫn là test rỗng. 20 kịch bản integration, 20 câu hỏi AI và 10 khách đồng thời là mục tiêu trong spec, chưa phải kết quả chạy.
- Kiểm chứng dữ liệu giữ nguyên sau tạo lại container, backup/restore, CI, README đầy đủ, deploy lên server.

## Trình tự cho phiên sau

1. Đọc notes và git status; giữ nguyên thay đổi của người dùng. Category.NormalizedName và mapping đã hoàn thiện, build thành công.
2. Khi làm luồng tạo/sửa sau này, chuẩn hóa tên bằng Trim().ToUpperInvariant(); entity không tự chuẩn hóa.
3. Không làm lại mapping Category. Chốt patch Npgsql khi cần build tái lập, không cài lại package máy móc.
4. Hướng dẫn EF tooling/design-time nếu cần, tạo và đọc migration, áp dụng bằng đúng host/port rồi kiểm tra bảng/constraint. Không thêm EnsureCreated hoặc migration ngầm.
5. Tiếp tục Product/quan hệ theo spec và kế hoạch 4 giờ/ngày; có thể điều chỉnh lịch theo tiến độ thực tế. Không tự nhảy sang ngày 2 chỉ vì đã sang 31/10 khi ngày 1 chưa xong.
6. Nếu người dùng muốn tiếp tục chỉnh tracker, làm trong PersonalProgress, không gắn tracker vào backend/Compose ShopFlow.
7. Cập nhật notes sau các mốc; phân biệt code đã có, build/test đã chạy và kết quả người dùng báo.

## Công cụ cá nhân PersonalProgress (tách khỏi ShopFlow)

- Cập nhật 31/10: đã sửa server.mjs, plan.mjs và index.html để khớp các nhóm việc đã hướng dẫn, giữ nguyên 120 ID và dữ liệu xác nhận. Nguồn kế hoạch chính là `docs/learning-plan.json` trong ShopFlow; plan.mjs là dự phòng. Đã kiểm tra giữ xác nhận/lưu trạng thái/đọc lại kế hoạch không restart bằng Node; đã rebuild và khởi động Docker thành công, kiểm tra API và HTML thực tế tại localhost:8091 trả nội dung mới. Chưa kiểm tra trực quan trình duyệt trong lần sửa này. Backup: `C:\Users\Hi\Documents\PersonalProgress\backup-aligned-plan-20261031-141141`. Docker CLI nằm tại `C:\Users\Hi\AppData\Local\Programs\DockerDesktop\resources\bin\docker.exe` (tìm được khi chạy ngoài sandbox).

- Người dùng yêu cầu web local tự cập nhật, sau đó chốt chạy bằng **Docker Compose riêng**, chỉ phục vụ theo dõi cá nhân. Không thêm service vào Compose ShopFlow, không sửa database/API ShopFlow để phục vụ tracker.
- Thư mục: `C:\Users\Hi\Documents\PersonalProgress`. File chính: `server.mjs` (Node HTTP server không dependency), `index.html`, `plan.mjs`, `Dockerfile`, `compose.yaml`, `.env`, `.dockerignore`, `README.md`.
- Compose project `personal-progress`, service `progress`, image build từ Node 24 Alpine. Cổng `127.0.0.1:8091:8091`, URL `http://localhost:8091`.
- `.env` công cụ chứa SHOPFLOW_PATH trỏ tới `C:/Users/Hi/Documents/C#/ShopFlow`. Mount vào `/project` **read-only**, dữ liệu xác nhận lưu ở named volume `progress-data` mount `/data`. Không dùng down -v nếu muốn giữ tiến độ.
- Tự đọc lại code cố định/notes/trạng thái mỗi 2 giây khi trang mở. Không đọc `.env` ShopFlow. Nhận diện theo mẫu chỉ cho biết có dấu hiệu code, không chứng minh build/test đúng. Notes được hiển thị tự cập nhật, không tự suy luận thành hoàn thành.
- Ban đầu có 19 mốc tổng quát và 7 mốc ghi nhận từ phiên học. Bản mới giữ chúng ở lịch sử cùng dữ liệu cũ, thêm **30 ngày × 4 giờ = 120 đầu việc**. Không tự đánh dấu bài học hoàn thành chỉ vì có code.
- Mỗi ngày: 45 phút hiểu bài + 90 phút thực hành + 75 phút kiểm chứng/sửa lỗi + 30 phút tổng kết; thời gian tập trung chưa tính nghỉ. Mỗi việc xác nhận riêng; đủ 4 việc mới xong ngày. Lưu timestamp xác nhận, hiển thị ngày còn tồn và mục tiêu cuối buổi.
- Mặc định từ **30/10/2026 đến 28/11/2026**, timezone hiển thị ngày hiện tại Asia/Ho_Chi_Minh. Đổi `PLAN_START_DATE=YYYY-MM-DD` trong `.env` công cụ rồi chạy lại Compose để dời lịch, các xác nhận gắn theo số buổi vẫn được giữ.
- Đây là ước lượng học tập, không cam kết MVP hoàn tất trong 120 giờ. Lịch: ngày 1 Category; 2 migration; 3–4 Product/mapping/seed; 5–8 auth; 9–14 catalog; 15 đệm/kiểm thử; 16–18 cart; 19–22 order/checkout/concurrency/hủy; 23 đệm/kiểm thử mua hàng; 24–26 React; 27 chatbot; 28 kiểm thử tổng hợp; 29 đóng gói/backup/CI; 30 demo/bàn giao. Nội dung chi tiết ở `plan.mjs`.
- Trợ lý đã kiểm tra bằng Node: 30 ngày/120 ID duy nhất, 240 phút mỗi ngày, ngày đầu/cuối, xác nhận đủ ngày, lưu trạng thái và giữ xác nhận cũ. Trước đó đã kiểm tra code/notes thay đổi được nhận ra không cần restart.
- **Chưa xác minh Docker build/chạy thực tế hoặc UI bằng trình duyệt:** shell không tìm thấy docker (cả đường dẫn Docker Desktop thường dùng), công cụ trình duyệt báo host unavailable. Người dùng đã được đưa lệnh rebuild nhưng chưa xác nhận bản mới chạy được.
- Có backup bản trước cập nhật lịch tại `C:\Users\Hi\Documents\PersonalProgress\backup-before-daily-20261030-235608`. `.env` và volume không bị ghi đè. Thư mục công cụ nằm ngoài workspace, thao tác ghi cần quyền theo môi trường; trước đó đã được duyệt cập nhật.
- Không có test server Node nào được chủ ý để chạy tiếp; các server thử đã dừng. Không tự khẳng định tracker đang chạy.

Lệnh chạy/cập nhật (thực hiện trong **PersonalProgress**, không phải ShopFlow):

```powershell
cd C:\Users\Hi\Documents\PersonalProgress
docker compose up -d --build
docker compose logs --tail 50 progress
```

## Git và tài liệu tham khảo

Chưa commit thay đổi phiên này. Tại lần kiểm tra cuối:

- Modified: spec, `src/ShopFlow.Api/Program.cs`, `src/ShopFlow.Api/ShopFlow.Api.csproj`.
- Untracked: `.dockerignore`, `Docker-compose.yaml`, `Dockerfile`, `docs/docker-checklist.md`, `notes.md`, `HealthController.cs`, thư mục `Data/`, `Entities/`, và tài liệu Word `docs/Thiết kế 2 dự án .NET cho CV thực tập sinh Backend.docx`. Không sửa tài liệu Word trong phiên này.
- Các commit đã ghi nhận trước phiên: `9bd3b03` khởi tạo; `4a81cec` spec ban đầu; `144c748` spec bàn phím/chatbot. Không tự commit khi chỉ đang hướng dẫn.

[Checklist Docker dùng chung](docs/docker-checklist.md): thứ tự Dockerfile/build/run/Compose/registry/server, bảng placeholder và lệnh theo container name/ID so với service name. Người dùng yêu cầu bản dùng được cho bất cứ dự án nào, không biến lại thành tài liệu riêng ShopFlow.

Nguồn học người dùng cung cấp: `C:\Users\Hi\Downloads\[PUBLIC-YT] Docker Siêu Tốc@hoidanit.md`.

Tài liệu Word tổng quan hai dự án: `C:\Users\Hi\Documents\C#\Thiết kế 2 dự án .NET cho CV thực tập sinh Backend.docx`; bản dự phòng `Thiết kế 2 dự án .NET cho CV thực tập sinh Backend.backup-2026-10-30.docx` cùng thư mục. Word có thể còn công nghệ cũ; ưu tiên quyết định mới trong notes và spec.
