# Docker — Các bước triển khai dùng chung

Tóm lược từ tài liệu Docker Siêu Tốc của Hỏi Dân IT, viết lại để tra cứu cho nhiều loại dự án. Các lệnh dưới đây là mẫu, chưa được chạy cho dự án cụ thể.

## Quy ước: cần thay gì trong câu lệnh?

**Thay toàn bộ phần `<...>` bằng giá trị thật, bỏ cả dấu `<` và `>`. Không gõ nguyên mẫu vào terminal.** Đường dẫn có khoảng trắng phải đặt trong dấu nháy kép.

| Ký hiệu | Điền gì? | Ví dụ |
|---|---|---|
| `<image>:<tag>` | Tên image và phiên bản do bạn đặt | `my-api:v1` |
| `<container>` | Tên HOẶC ID container đã tồn tại | `api-demo` hoặc `a1b2c3d4` |
| `<container-name>` | Tên mới do bạn đặt khi tạo container | `api-demo` |
| `<service>` | Tên dưới `services:` trong file Compose | `api`, `db` |
| `<host-port>` | Cổng truy cập trên máy chạy Docker | `8080` |
| `<container-port>` | Cổng ứng dụng thực sự lắng nghe trong container | `3000` |
| `<volume>` | Tên volume do bạn đặt | `db-data` |
| `<data-path>` | Thư mục dữ liệu bên trong container, theo image | `/var/lib/mysql` |
| `<network>` | Tên mạng Docker do bạn đặt | `app-net` |
| `<dockerfile-path>` | Đường dẫn Dockerfile | `./backend/Dockerfile` |
| `<context>` | Thư mục chứa dữ liệu để Docker COPY khi build | `.` hoặc `./backend` |
| `<command>` | Lệnh/chương trình có sẵn bên trong container | `sh`, `node --version` |

Tra tên/ID container: `docker ps -a`. Tra image: `docker image ls`. Tra service: `docker compose config --services`.

**Phân biệt:** `docker logs <container>` dùng tên/ID container; `docker compose logs <service>` dùng tên service. Image là bản đóng gói; container là phiên chạy từ image.

## 1. Chuẩn bị

- Cài và mở Docker; chọn chế độ container phù hợp với image (thường là Linux).
- Xác định lệnh build, lệnh chạy, cổng và biến môi trường của ứng dụng. Kiểm tra ứng dụng chạy được.

```powershell
docker version
docker compose version
```

## 2. Chọn các thành phần chạy riêng

Ví dụ: frontend, backend, database. Chỉ viết Dockerfile cho thành phần cần tự đóng gói. Database thường dùng image có sẵn. FE tĩnh có thể chạy riêng hoặc được backend phục vụ chung.

## 3. Tạo Dockerfile và .dockerignore

Dockerfile phải phù hợp ngôn ngữ/framework, không có một nội dung dùng nguyên xi cho mọi dự án.

| Chỉ dẫn | Dùng để làm gì? |
|---|---|
| `FROM` | Chọn image nền, kèm phiên bản |
| `WORKDIR` | Chọn thư mục làm việc bên trong image |
| `COPY` | Chép file từ build context vào image |
| `RUN` | Chạy lệnh lúc build: cài thư viện, biên dịch… |
| `COPY --from=build` | Lấy kết quả từ stage tên `build` |
| `ENV` | Đặt biến môi trường mặc định, không chứa secret |
| `EXPOSE` | Ghi nhận cổng; không tự publish cổng |
| `USER` | Chọn tài khoản chạy ứng dụng |
| `ENTRYPOINT` / `CMD` | Định nghĩa chương trình và tham số mặc định khi chạy container |

Thứ tự thường dùng: image build → chép file khai báo thư viện → cài thư viện → chép code → build → chép kết quả sang image runtime → đặt lệnh chạy.

`.dockerignore`: loại `.git`, thư viện/build local và file secret như `.env`. Tạo `.env.example` không chứa secret; thêm `.env` thật vào `.gitignore`.

## 4. Build image

```powershell
docker build -f <dockerfile-path> -t <image>:<tag> <context>
```

Ví dụ chạy từ gốc repo, Dockerfile nằm trong backend nhưng được phép COPY file từ cả repo:

```powershell
docker build -f ./backend/Dockerfile -t my-api:v1 .
```

`-f`: đường dẫn Dockerfile. `-t`: tên image và tag. Dấu `.` cuối lệnh là build context, không phải tên container. Đường dẫn nguồn trong `COPY` tính từ context.

## 5. Chạy thử một container

```powershell
docker run -d --name <container-name> -p <host-port>:<container-port> --env-file <env-file-path> <image>:<tag>
```

Ví dụ (ứng dụng trong image đã được cấu hình nghe cổng 3000):

```powershell
docker run -d --name api-demo -p 8080:3000 --env-file .env my-api:v1
docker logs api-demo
```

- `-d`: chạy nền. `--name`: đặt tên container mới, không điền ID.
- `-p 8080:3000`: truy cập máy tại cổng 8080 → ứng dụng trong container tại cổng 3000.
- `--env-file`: đường dẫn file biến môi trường; bỏ tùy chọn này nếu không dùng.
- `my-api:v1`: image để tạo container, không phải tên container.
- Mở `http://localhost:8080` nếu là ứng dụng HTTP. Ứng dụng phải nghe trên interface cho phép truy cập từ ngoài container, thường là `0.0.0.0`.

Nếu chạy nhiều thành phần bằng tay: tạo mạng bằng `docker network create <network>`, thêm `--network <network>` vào từng lệnh `docker run`. Database thêm `-v <volume>:<data-path>` để giữ dữ liệu. Không cần làm thủ công khi dùng Compose ở bước tiếp theo.

## 6. Chuyển cấu hình sang docker-compose.yml

Khai báo từng thành phần dưới `services:`:

| Thuộc tính | Nội dung cần điền |
|---|---|
| `build.context`, `build.dockerfile` | Context và đường dẫn Dockerfile (đường dẫn tương đối Dockerfile tính từ context) |
| `image` | Image có sẵn hoặc tên image muốn build |
| `ports` | `"cổng-máy:cổng-container"` |
| `environment` / `env_file` | Biến môi trường truyền vào container |
| `volumes` | `tên-volume:thư-mục-dữ-liệu` và khai báo named volume ở cấp gốc |
| `healthcheck` | Lệnh kiểm tra phù hợp, có sẵn trong image |
| `depends_on` | Nếu chờ database sẵn sàng, dùng `condition: service_healthy` cùng healthcheck của database |
| `restart` | Ví dụ `unless-stopped` |

Compose tự tạo mạng chung. Backend gọi database bằng **tên service và cổng bên trong container**; `localhost` trong container là chính container đó. Frontend chạy trong trình duyệt gọi địa chỉ API truy cập được từ trình duyệt, không gọi tên service Docker.

File `.env` phục vụ thay biến trong Compose; muốn truyền biến vào container vẫn cần `environment` hoặc `env_file`. Không đưa secret vào frontend build vì trình duyệt có thể đọc được.

## 7. Khởi động và kiểm tra cả hệ thống

Chạy trong thư mục chứa file Compose. Nếu file ở nơi khác, dùng `docker compose -f <compose-file-path> ...`.

```powershell
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs --tail 100 <service>
```

Nếu container chạy thử ở bước 5 đang chiếm cổng, dừng nó bằng `docker stop <container>` trước.

Sau khi database sẵn sàng: chạy migration/seed theo framework dự án, rồi kiểm tra web/API và thao tác đọc/ghi dữ liệu. Không có một lệnh migration chung cho mọi ngôn ngữ; image runtime có thể không chứa công cụ migration.

Tạo dữ liệu thử → `docker compose down` → `docker compose up -d` → kiểm tra dữ liệu còn. **Không thêm `-v` vào `down` khi cần giữ volume dữ liệu.** Volume không thay thế backup.

## 8. Các lệnh sử dụng lại

| Việc cần làm | Container chạy riêng: dùng tên/ID | Compose: dùng tên service |
|---|---|---|
| Xem trạng thái | `docker ps -a` | `docker compose ps -a` |
| Theo dõi log | `docker logs -f <container>` | `docker compose logs -f <service>` |
| Chạy lệnh bên trong | `docker exec -it <container> <command>` | `docker compose exec <service> <command>` |
| Dừng | `docker stop <container>` | `docker compose stop <service>` |
| Chạy lại cái đã dừng | `docker start <container>` | `docker compose start <service>` |
| Khởi động lại | `docker restart <container>` | `docker compose restart <service>` |
| Xóa container đã dừng | `docker rm <container>` | `docker compose down` dỡ cả hệ thống, giữ named volume |

Ví dụ hai cách mở shell (chọn cách phù hợp; image phải có `sh`):

```powershell
docker exec -it api-demo sh
docker compose exec api sh
```

Sau khi sửa code, với Compose:

```powershell
docker compose up -d --build
```

Nếu chạy riêng: build image mới → dừng/xóa container cũ → chạy container mới với cấu hình đã lưu. `restart` không cập nhật image hoặc cấu hình container.

## 9. Đưa lên máy chủ (khi cần)

`<registry-user>` là username Docker Hub, `<repository>` là tên kho image; không điền container name/ID. Nếu dùng registry khác, thêm hostname registry vào tên image.

Trên máy build (lặp lại cho từng image tự đóng gói):

```powershell
docker tag <local-image>:<tag> <registry-user>/<repository>:<tag>
docker login
docker push <registry-user>/<repository>:<tag>
```

Trên máy chủ:

1. Cài Docker/Compose; image phải hỗ trợ hệ điều hành/kiến trúc máy chủ.
2. Chép file Compose triển khai; dùng `image: username/repository:tag` thay phần `build` cho ứng dụng.
3. Tạo cấu hình/secret riêng trên máy chủ; đăng nhập registry nếu image riêng tư.
4. Chạy các lệnh dưới; migration theo quy trình dự án, backup database trước thay đổi schema có dữ liệu.

```powershell
docker compose pull
docker compose up -d
docker compose ps
docker compose logs --tail 100 <service>
```

Nếu public Internet: cấu hình domain, HTTPS/reverse proxy, firewall và backup; chỉ mở các cổng cần thiết. Mỗi bản phát hành dùng tag mới, sửa tag trong Compose rồi pull/up lại. Push image lên registry chưa làm website chạy: cần máy chủ chạy container.
