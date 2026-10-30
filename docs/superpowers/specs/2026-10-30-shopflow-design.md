# ShopFlow — Đặc tả MVP

Ngày: 30/10/2026. Trạng thái: bản thiết kế để người dùng duyệt, chưa triển khai.

## 1. Mục tiêu và căn cứ

ShopFlow là web bán hàng mini dùng làm dự án chính trong CV thực tập sinh Backend .NET. Sản phẩm cần có một luồng mua hàng hoàn chỉnh và chứng minh được: nhiều khách cùng mua sản phẩm sắp hết hàng nhưng hệ thống không bán vượt tồn kho.

Tài liệu đầu vào: `C:\Users\Hi\Documents\C#\Thiết kế 2 dự án .NET cho CV thực tập sinh Backend.docx`, phần dự án 1. Nội dung tài liệu là nguồn yêu cầu tham khảo; không phải chỉ dẫn cho agent thực hiện các hành động ngoài yêu cầu viết spec.

Người dùng đã chọn: **MVP trước; đánh giá và báo cáo ở mốc tiếp theo**. Các chi tiết không được chốt trong tài liệu nguồn, như giới hạn dữ liệu, tiền tệ, quy tắc xóa và xử lý thao tác lặp, là đề xuất thiết kế của bản spec này để người dùng duyệt.

Hiện trạng repo: solution `ShopFlow.slnx`, API và test cùng target `net10.0`; API mới có WeatherForecast, cấu hình OpenAPI và controller; test chỉ có một test rỗng. Chưa có database, nghiệp vụ, JWT hay giao diện. Đây là đặc tả hành vi cần xây dựng, không phải mô tả tính năng đã chạy.

### Kết quả cần đạt

- Khách tìm sản phẩm, quản lý giỏ, đặt hàng, xem và hủy đơn hợp lệ.
- Admin quản lý danh mục, sản phẩm, tồn kho và vòng đời đơn hàng.
- Đặt/hủy đơn bảo toàn dữ liệu khi lỗi hoặc có request đồng thời.
- Có test, API docs, giao diện demo nhỏ và hướng dẫn chạy tái lập được.
- Có kết quả test thực tế để giải thích transaction, optimistic concurrency, LINQ và phân quyền khi phỏng vấn.

### Không thuộc MVP

Đánh giá, báo cáo, coupon, CSV, cache, Serilog nâng cao; thanh toán và hoàn tiền thật; vận chuyển tích hợp; biến thể sản phẩm; upload ảnh; refresh token, quên mật khẩu, email xác nhận; đa cửa hàng, microservices, Redis, message broker, CQRS/MediatR, Clean Architecture nhiều project. Không thêm công nghệ chỉ để tăng số mục trong CV.

## 2. Phương án và quyết định

| Phương án | Đánh đổi | Kết luận |
|---|---|---|
| MVP gồm mua hàng và quản trị; đánh giá/báo cáo làm sau | Có bản demo sớm; MVP chỉ có 7 bảng nghiệp vụ | Người dùng đã chọn |
| Gộp đánh giá và báo cáo vào bản đầu | Đủ 8 bảng như checklist nguồn nhưng tăng thời gian và diện tích kiểm thử | Hoãn đến mốc 2 |
| Chỉ backend API | Nhanh hơn nhưng khó trình diễn cho người không dùng Swagger | Không chọn làm phạm vi giao hàng; có thể phát triển API trước |

Giữ kiến trúc Controller–Service–Repository của tài liệu nguồn trong một project API. So với Clean Architecture nhiều project, cách này ít lớp trung gian hơn và đủ để trình bày phân tách trách nhiệm. So với đặt toàn bộ logic trong controller, service và entity giúp kiểm thử nghiệp vụ độc lập. Đây là đề xuất kỹ thuật dựa trên nguồn và repo hiện có.

## 3. Công nghệ và cấu trúc

| Thành phần | Quyết định |
|---|---|
| API | ASP.NET Core Web API .NET 10, controller, async/await, DI |
| Dữ liệu | EF Core 10 và SQL Server; migrations quản lý schema |
| Xác thực | JWT Bearer; PasswordHasher của ASP.NET Core để hash/verify mật khẩu |
| Tài liệu API | OpenAPI và Swagger UI có Bearer Authorize |
| Test | xUnit hiện có; chọn một trong Moq/NSubstitute khi cần mock |
| Test tích hợp | API test host và SQL Server thật trong Docker |
| Demo | HTML/CSS/JavaScript thuần gọi fetch, phục vụ cùng origin với API |
| Chạy và CI | Docker Compose API + SQL Server; GitHub Actions build/test |

Phiên bản patch và thư viện Swagger UI cụ thể được chốt lúc lập kế hoạch theo độ tương thích .NET 10; không tự đổi framework của repo.

```text
src/ShopFlow.Api/
  Controllers/       # HTTP, authorization, DTO mapping
  Services/          # Use case, transaction, phối hợp repository
  Repositories/      # Truy vấn EF Core, không tự commit từng thao tác
  Entities/          # Entity, enum, invariant và chuyển trạng thái
  Data/              # DbContext, mapping, migrations, seed
  Dtos/              # Request/response; không trả entity trực tiếp
  Middlewares/       # Xử lý lỗi tập trung
  Security/          # Token và thông tin người dùng hiện tại
  wwwroot/           # Tài sản demo từ web khi build/publish
  Program.cs
tests/ShopFlow.Tests/ # Unit tests hiện có, thay test rỗng
tests/ShopFlow.IntegrationTests/
web/                 # Mã nguồn HTML/CSS/JS demo
docs/superpowers/specs/
docker-compose.yml
README.md
```

Service dùng chung một scoped DbContext thông qua repository và đơn vị commit nhỏ. Repository không tự mở transaction hoặc gọi SaveChanges cho mỗi insert/update. Không cần generic repository bao toàn bộ EF Core. Truy vấn đọc lọc/sort/page ở database trước khi materialize; không lộ IQueryable ra controller. Logic như `Order.MarkAsPaid()` và `Order.Cancel()` kiểm soát vòng đời; service kiểm tra quyền và điều phối hoàn kho.

## 4. Vai trò và hành trình

| Vai trò | Được phép |
|---|---|
| Anonymous | Đăng ký/đăng nhập, xem danh mục và sản phẩm đang bán |
| Customer | Các quyền đọc công khai; quản lý giỏ của mình; tạo, xem, hủy đơn của mình theo quy tắc |
| Admin | Quản trị danh mục/sản phẩm; xem mọi đơn và cập nhật trạng thái |

Admin không mặc nhiên được dùng các endpoint chỉ dành cho Customer. Muốn demo mua hàng, dùng tài khoản Customer riêng. UserId của mọi thao tác khách hàng lấy từ token, không nhận từ request body.

Luồng khách: đăng ký → đăng nhập → tìm sản phẩm → thêm/sửa giỏ → xác nhận địa chỉ → đặt hàng → xem chi tiết → hủy nếu còn Pending.

Luồng admin: đăng nhập → tạo danh mục/sản phẩm và số tồn → xem đơn Pending → xác nhận Paid → Shipped → Completed; có thể hủy Pending/Paid. Paid là xác nhận thanh toán thủ công phục vụ demo, không phát sinh giao dịch tiền thật.

## 5. Quy tắc nghiệp vụ MVP

### 5.1 Tài khoản

- Đăng ký luôn tạo Customer; request không có trường Role. Tạo User và giỏ trống trong cùng transaction.
- Email trim và chuẩn hóa chữ thường vào NormalizedEmail; unique ở database. FullName trim, 2–100 ký tự; email tối đa 254 ký tự và đúng định dạng.
- Mật khẩu 8–128 ký tự, không trim; dùng password hasher, không lưu/log plaintext hay tự hash bằng SHA thuần.
- Email trùng trả 409; đăng nhập sai trả 401 với thông báo chung.
- Token có định danh người dùng, role, issuer, audience, thời hạn; mặc định 60 phút. Kiểm tra chữ ký, issuer, audience và lifetime. Đây là cơ chế cho demo khép kín; chưa thiết kế dịch vụ định danh công khai.
- Admin được seed bằng cấu hình môi trường ở Development/Demo; không có API đăng ký admin. Secret và mật khẩu seed không commit.
- Giao diện giữ token trong bộ nhớ, đăng xuất xóa token; reload trang cần đăng nhập lại. Không có thu hồi token phía server trong MVP.

### 5.2 Danh mục và sản phẩm

- Tên danh mục trim, 1–100 ký tự; NormalizedName unique. Danh mục có sản phẩm tham chiếu, kể cả sản phẩm ngừng bán, không được xóa: 409.
- Tên sản phẩm 1–200 ký tự, mô tả tối đa 2.000 ký tự; CategoryId phải tồn tại.
- Tiền tệ duy nhất VND; dùng decimal và `decimal(18,2)`, chỉ nhận số tiền nguyên VND trong MVP. Giá từ 1 đến 1.000.000.000; Stock nhập qua API quản trị là số nguyên từ 0 đến 1.000.000. Giới hạn 1.000.000 chỉ áp dụng đầu vào quản trị, không áp dụng kết quả hoàn kho: tồn sau hủy có thể cao hơn nếu admin đã bổ sung hàng. Invariant lưu trữ là Stock không âm; dùng bigint/C# long và phép toán checked, lỗi vượt miền kiểu số phải rollback toàn bộ.
- DELETE sản phẩm là ngừng bán (`IsActive=false`), không xóa vật lý; admin có thể kích hoạt lại bằng PUT. Đơn cũ vẫn đọc được.
- Khách chỉ thấy sản phẩm active; sản phẩm hết hàng vẫn hiển thị với Stock=0. Chi tiết sản phẩm inactive trả 404 cho khách; admin có endpoint đọc riêng.
- PUT sản phẩm cập nhật cả thông tin và số tồn tuyệt đối, yêu cầu RowVersion đã đọc gần nhất. Bản sửa cũ không được ghi đè tồn kho sau một lần bán: 409.
- GET danh sách: `q` tối đa 100 ký tự, tìm Contains theo tên; `categoryId`, `minPrice`, `maxPrice`; `sort` thuộc `newest`, `priceAsc`, `priceDesc`, `nameAsc`. Mặc định newest; thêm Id làm khóa phụ ổn định.
- `page=1`, `size=20` mặc định; page >=1, size 1–100. Khoảng giá không hợp lệ hoặc sort không hỗ trợ trả 400; trang vượt phạm vi trả items rỗng.
- Quy tắc so khớp chữ/dấu theo collation SQL Server được ghi trong README; không cam kết tìm kiếm bỏ dấu.

### 5.3 Giỏ hàng

- Một giỏ cho mỗi Customer, tối đa 50 sản phẩm khác nhau; mỗi dòng Quantity 1–99.
- POST thêm số lượng vào dòng hiện có; PUT đặt số lượng tuyệt đối; DELETE xóa dòng. Quantity=0 không thay cho xóa.
- Không nhận giá từ client. GET giỏ tính tổng tạm tính từ giá hiện tại; không đảm bảo giữ giá hoặc giữ kho.
- Thêm/sửa yêu cầu sản phẩm active và tổng số lượng dòng không vượt Stock hiện tại; thiếu kho hoặc inactive trả 409. Sau đó tồn kho vẫn có thể thay đổi.
- Sản phẩm ngừng bán/thiếu kho vẫn hiện trong giỏ đã có, kèm `isAvailable` và số tồn để người dùng sửa/xóa. Checkout kiểm tra lại toàn bộ giỏ.
- Mỗi thao tác ghi giỏ và checkout đều tăng Carts.Revision. Revision là concurrency token, nhằm chặn hai thao tác trên cùng giỏ ghi đè hoặc checkout một giỏ đã thay đổi trong lúc xử lý.
- Xóa dòng không có trong giỏ của mình trả 404; không cho phép truy cập giỏ bằng UserId do client cung cấp.

### 5.4 Đặt hàng

- Request chỉ chứa `shippingAddress` (trim, 10–500 ký tự). Mua toàn bộ giỏ; không hỗ trợ mua một phần.
- Giỏ rỗng trả 400. Sản phẩm thiếu kho/inactive trả 409; không tạo đơn một phần.
- Service mở transaction, đọc giỏ cùng Revision, đọc sản phẩm cùng RowVersion, kiểm tra điều kiện; tạo Order Pending và OrderItems, trừ kho, xóa các dòng giỏ và tăng Revision; commit toàn bộ.
- OrderItem lưu ProductName và UnitPrice tại lúc checkout. Tổng tiền bằng tổng `UnitPrice × Quantity`, backend tự tính; chưa có thuế/phí vận chuyển/giảm giá.
- Dùng giá hiện hành khi checkout; giao diện nêu rõ giá được tính lại khi đặt hàng và hiển thị tổng cuối cùng từ response. Không nhận giá client làm giá chính thức.
- Bất kỳ xung đột ghi nào phải rollback toàn bộ, trả 409; thất bại không được tạo Order, trừ một phần kho hoặc làm mất giỏ.
- Thành công trả 201 với chi tiết đơn và Location. Sau khi commit, giỏ rỗng.
- Không tự retry checkout khi xung đột hoặc mất kết nối. Client tải lại giỏ/lịch sử; timeout có thể xảy ra sau commit. MVP không cam kết idempotency key cho mạng gửi lại; không tự gửi lại POST ngầm.

### 5.5 Vòng đời đơn và hoàn kho

| Từ | Sang | Chủ thể | Tác động |
|---|---|---|---|
| Pending | Paid | Admin | Ghi PaidAt theo UTC |
| Pending | Cancelled | Chủ đơn Customer hoặc Admin | Hoàn kho đúng một lần |
| Paid | Shipped | Admin | Ghi ShippedAt |
| Paid | Cancelled | Admin | Hoàn kho; đánh dấu hủy thanh toán mô phỏng |
| Shipped | Completed | Admin | Ghi CompletedAt |

Completed và Cancelled là trạng thái kết thúc. Không nhảy cóc hoặc quay lui; chuyển không hợp lệ trả 409. Không tự hết hạn Pending trong MVP, nên kho đã trừ được giữ cho đến khi đơn được xử lý/hủy.

Customer chỉ hủy được Pending. Gọi lại cancel cho đơn đã Cancelled của mình trả 200 và giữ nguyên dữ liệu. Admin gửi lại trạng thái hiện tại trả 200, không thay timestamp hay cộng kho lần nữa. Các trường hợp khác tuân thủ bảng trên.

Hủy đơn phải cập nhật trạng thái, CancelledAt và hoàn toàn bộ số lượng trong cùng transaction. Products inactive vẫn được hoàn kho. Giữ PaidAt nếu hủy sau Paid để bảo toàn lịch sử; không tuyên bố đã hoàn tiền thật. Không đưa các món đã hủy trở lại giỏ.

Orders có RowVersion để chặn hai request hủy hoặc hủy/xác nhận thanh toán cùng thắng. Products có RowVersion để chặn hoàn kho ghi đè một lần bán khác. Nếu xung đột, rollback rồi trả 409 để client tải lại; yêu cầu lặp đến sau khi lần đầu hoàn tất áp dụng quy tắc idempotent phía trên.

## 6. Dữ liệu và ràng buộc

ID dùng int identity; timestamp UTC dùng DateTimeOffset; role/status lưu chuỗi có giới hạn và check constraint. Mọi trường mặc định NOT NULL trừ các timestamp chưa xảy ra. RowVersion là SQL Server rowversion (byte[]), không phải thời gian; dạng truyền qua DTO admin là Base64.

| Bảng | Trường và ràng buộc chính |
|---|---|
| Users | Id, Email nvarchar(254), NormalizedEmail nvarchar(254) unique, PasswordHash nvarchar(512), FullName nvarchar(100), Role nvarchar(20), CreatedAt |
| Categories | Id, Name nvarchar(100), NormalizedName nvarchar(100) unique |
| Products | Id, CategoryId FK, Name nvarchar(200), Description nvarchar(2000), Price decimal(18,2), Stock bigint, IsActive bit, CreatedAt, RowVersion; check Price>0, Stock>=0 |
| Carts | Id, UserId FK unique, Revision bigint mặc định 0, cấu hình concurrency token |
| CartItems | Id, CartId FK, ProductId FK, Quantity int; unique(CartId,ProductId); check Quantity từ 1 đến 99 |
| Orders | Id, UserId FK, Status nvarchar(20), TotalAmount decimal(18,2), ShippingAddress nvarchar(500), CreatedAt, PaidAt?, ShippedAt?, CompletedAt?, CancelledAt?, RowVersion; check TotalAmount>0 |
| OrderItems | Id, OrderId FK, ProductId FK, ProductName nvarchar(200), UnitPrice decimal(18,2), Quantity int; unique(OrderId,ProductId); check UnitPrice>0, Quantity từ 1 đến 99 |

Products.Description mặc định chuỗi rỗng. Index: Products(CategoryId), Products(Name), Orders(UserId,CreatedAt), Orders(Status,CreatedAt), OrderItems(ProductId). Không hứa index Name sẽ tối ưu được Contains; chỉ đo và điều chỉnh khi có nhu cầu thực tế.

Quan hệ: User 1–1 Cart; Cart 1–n CartItem; Category 1–n Product; User 1–n Order; Order 1–n OrderItem; Product 1–n CartItem và OrderItem. Không cascade xóa Product/User vào lịch sử đơn. Không có endpoint xóa user/order trong MVP. CartItem có thể cascade khi xóa Cart; OrderItem được cấu hình restrict để tránh mất lịch sử do thao tác ngoài dự kiến.

MVP có **7 bảng nghiệp vụ**. Reviews là bảng thứ 8 ở mốc 2; bảng lịch sử migrations không được tính để làm đủ chỉ tiêu. Đây là điều chỉnh có chủ đích so với checklist “ít nhất 8 bảng” của nguồn, phù hợp lựa chọn MVP của người dùng.

## 7. Hợp đồng API

Prefix `/api`; JSON camelCase, enum status dạng chuỗi. Mọi ID route phải là số nguyên dương. PUT là cập nhật đầy đủ các trường được phép sửa; client không gửi entity hoặc trường hệ thống.

| Method | Route | Quyền | Kết quả chính |
|---|---|---|---|
| POST | /auth/register | Public | 201: user và token |
| POST | /auth/login | Public | 200: user và token |
| GET | /auth/me | Đã đăng nhập | 200: id, email, fullName, role |
| GET | /categories | Public | 200: danh sách id, name |
| GET | /products | Public | 200: phân trang sản phẩm active |
| GET | /products/{id} | Public | 200 hoặc 404 |
| GET | /admin/categories | Admin | 200: danh sách danh mục |
| GET | /admin/categories/{id} | Admin | 200: chi tiết danh mục, đích Location khi tạo |
| POST | /admin/categories | Admin | 201, Location |
| PUT | /admin/categories/{id} | Admin | 200 |
| DELETE | /admin/categories/{id} | Admin | 204 hoặc 409 nếu còn sản phẩm |
| GET | /admin/products | Admin | 200: phân trang, gồm inactive; lọc isActive tùy chọn |
| GET | /admin/products/{id} | Admin | 200: có rowVersion |
| POST | /admin/products | Admin | 201, Location |
| PUT | /admin/products/{id} | Admin | 200; yêu cầu rowVersion trong body |
| DELETE | /admin/products/{id} | Admin | 204: ngừng bán; lặp lại vẫn 204 |
| GET | /cart/items | Customer | 200: giỏ, các dòng, tổng tạm tính |
| POST | /cart/items | Customer | 200: giỏ mới; body productId, quantity |
| PUT | /cart/items/{productId} | Customer | 200: giỏ mới; body quantity |
| DELETE | /cart/items/{productId} | Customer | 204 |
| POST | /orders | Customer | 201: chi tiết đơn; body shippingAddress |
| GET | /orders/me | Customer | 200: phân trang đơn của mình, lọc status |
| GET | /orders/{id} | Customer | 200: đơn của mình; đơn người khác trả 404 |
| POST | /orders/{id}/cancel | Customer | 200: chi tiết đơn sau hủy |
| GET | /admin/orders | Admin | 200: phân trang, lọc status và userId |
| GET | /admin/orders/{id} | Admin | 200: chi tiết bất kỳ đơn |
| PUT | /admin/orders/{id}/status | Admin | 200: chi tiết sau cập nhật; body status |

Danh sách đơn sắp CreatedAt giảm dần, Id giảm dần; dùng page/size và giới hạn như sản phẩm. Status không thuộc enum trả 400. Mọi route quản trị thiếu token trả 401, sai role trả 403. Kiểm tra quyền sở hữu trước khi tiết lộ chi tiết đơn.

Product create/update body: `categoryId`, `name`, `description`, `price`, `stock`, `isActive`; update thêm `rowVersion`. Category body: `name`. Register body: `email`, `password`, `fullName`; login: `email`, `password`.

DELETE sản phẩm đọc RowVersion trong request scope, chỉ sửa IsActive; không ghi đè các trường khác. Nếu có ghi đồng thời trong lúc xử lý, trả 409. Mọi endpoint ghi trả 404 nếu tài nguyên đích không tồn tại, trừ các hành vi lặp đã nêu.

Response chuẩn:

```json
{
  "items": [],
  "page": 1,
  "size": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

Token response: `{accessToken, tokenType:"Bearer", expiresAt, user:{id,email,fullName,role}}`. Giỏ: `{id, items:[{productId,productName,unitPrice,quantity,lineTotal,stock,isActive,isAvailable}], totalAmount}`. Đơn chi tiết: `{id,status,totalAmount,shippingAddress,createdAt,paidAt,shippedAt,completedAt,cancelledAt,items:[{productId,productName,unitPrice,quantity,lineTotal}]}`. Danh sách đơn trả các trường tóm tắt, không nhúng toàn bộ items. UserId được thêm vào DTO quản trị đơn.

### Lỗi

Lỗi dùng ProblemDetails: `type`, `title`, `status`, `detail`, `instance`, thêm `code` ổn định và `traceId`; validation thêm `errors` theo field. Type mặc định `about:blank`. Không đưa SQL, stack trace, token hoặc password vào response/log.

| HTTP | Trường hợp / code mẫu |
|---|---|
| 400 | VALIDATION_ERROR, EMPTY_CART, status/query không hợp lệ |
| 401 | Token thiếu/hỏng/hết hạn; INVALID_CREDENTIALS |
| 403 | FORBIDDEN: đúng danh tính nhưng sai role |
| 404 | NOT_FOUND: tài nguyên không có hoặc đơn không thuộc khách |
| 409 | EMAIL_EXISTS, CATEGORY_EXISTS, CATEGORY_IN_USE, INSUFFICIENT_STOCK, PRODUCT_UNAVAILABLE, CONCURRENCY_CONFLICT, INVALID_ORDER_TRANSITION, CART_LIMIT_EXCEEDED |
| 429 | RATE_LIMITED ở endpoint đăng ký/đăng nhập |
| 500 | INTERNAL_ERROR: lỗi chưa dự kiến, log với traceId |

Áp dụng cùng định dạng cho lỗi validation tự động và auth challenge/forbid. Unique violation khi đăng ký/tạo danh mục đồng thời được ánh xạ đúng 409. Hai thao tác thêm cùng một sản phẩm vào giỏ có thể chạm unique(CartId,ProductId) trước khi phát hiện Revision; ánh xạ đúng constraint này thành 409 CONCURRENCY_CONFLICT và rollback toàn bộ. Không coi mọi DbUpdateException là lỗi trùng hoặc lỗi concurrency.

## 8. Đồng thời và tính nguyên tử

Ba phạm vi bảo vệ khác nhau:

1. **Products.RowVersion** bảo vệ số tồn và giá/thông tin được đọc trong checkout; version đổi trước khi ghi làm thao tác thất bại.
2. **Orders.RowVersion** bảo vệ trạng thái, đặc biệt hủy đồng thời với hủy hoặc Paid.
3. **Carts.Revision** bảo vệ toàn bộ tập dòng giỏ; mọi ghi CartItems phải đi kèm tăng Revision trong cùng transaction. Khi checkout đọc giỏ phải đọc parent Revision trước các dòng; request dùng context riêng và không tái sử dụng snapshot cũ.

Thiết kế dùng optimistic concurrency và transaction ngắn ở mức ReadCommitted mặc định; không dùng lock trong bộ nhớ làm bảo đảm đúng. Quy tắc token và xử lý DbUpdateConcurrencyException dựa trên [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).

Tất cả thay đổi của một use case checkout/cancel là một transaction. Trên conflict, rollback toàn bộ transaction và kết thúc request, không tiếp tục lưu bằng change tracker đã lỗi. Không bật retry tự động cho luồng này trong MVP; nếu sau này dùng execution strategy, phải thiết kế retry cho toàn bộ đơn vị transaction. Căn cứ cơ chế atomicity và giới hạn execution strategy: [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

Giới hạn: optimistic concurrency có thể từ chối một thao tác dù vẫn còn hàng; chấp nhận để client đọc lại. Tránh gọi dịch vụ ngoài hoặc render giao diện trong transaction. Không dùng SQLite hoặc EF InMemory làm bằng chứng SQL Server rowversion hoạt động đúng.

## 9. Giao diện demo và vận hành

MVP có các màn hình tối thiểu: đăng ký/đăng nhập; sản phẩm và bộ lọc; giỏ và địa chỉ checkout; danh sách/chi tiết đơn; admin danh mục/sản phẩm; admin danh sách/chi tiết và trạng thái đơn. Không yêu cầu thiết kế thương mại hoàn chỉnh hoặc React.

- Ẩn thao tác không phù hợp role/trạng thái; backend vẫn kiểm tra độc lập.
- Chặn bấm submit liên tục để cải thiện UX; không coi đây là biện pháp chống trùng dữ liệu.
- Khi 409, hiển thị lỗi dễ hiểu và tải lại dữ liệu; không tự gửi lại checkout. Khi 401, yêu cầu đăng nhập lại.
- Hiển thị giá VND, trạng thái rỗng/đang tải/lỗi; dùng textContent với dữ liệu người dùng để tránh chèn HTML.
- Demo cùng origin giúp không cần CORS rộng; nếu chạy web riêng, chỉ cho phép origin cấu hình.

Docker Compose có API và SQL Server với volume dữ liệu, healthcheck và cấu hình môi trường. API phục vụ cả web và Swagger UI. Seed có danh mục, sản phẩm mẫu, một admin cấu hình từ môi trường; chạy seed nhiều lần không nhân bản. Migration tự động chỉ cho local/demo đơn instance; môi trường triển khai khác dùng bước migration riêng được mô tả.

README cung cấp `.env.example` không chứa secret thật, yêu cầu môi trường, lệnh chuẩn bị và `docker compose up --build`, cách reset dữ liệu có cảnh báo rõ. SQL Server healthy trước bước migration/seed. Container local có thể dùng HTTP qua loopback được cấu hình rõ; triển khai công khai phải có HTTPS và secret riêng.

Health endpoint `/health` trả 200 khi API/database sẵn sàng, 503 khi database không dùng được; không lộ connection string. Đây là kiểm tra sẵn sàng tối thiểu để chạy Compose, khác bộ health/observability nâng cao để sau.

Đăng nhập/đăng ký giới hạn mặc định 10 request/phút/IP trong demo đơn instance; 429 có Retry-After. Log request lỗi có traceId, thời gian và loại lỗi, không log body đăng nhập/địa chỉ đầy đủ. Cấu hình JWT validation theo [hướng dẫn ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

CI build và chạy unit tests mỗi push/PR; integration tests dùng SQL Server service/container riêng, không dùng database thật của người dùng. Không đặt chỉ tiêu latency/throughput chưa đo; tối ưu sau khi có số liệu. Mọi endpoint danh sách lớn phải phân trang.

## 10. Kiểm thử và tiêu chí nghiệm thu

### Unit tests bắt buộc

Ít nhất 10 test có ý nghĩa, bao phủ: tính tổng nhiều dòng; giữ snapshot giá/tên; thiếu kho; số lượng không hợp lệ; từng chuyển trạng thái hợp lệ; chuyển sai; khách hủy Paid bị từ chối; hủy hoàn kho; lặp hủy không hoàn thêm; phân biệt quyền sở hữu. Tách unit test entity và service; không dùng mock để kết luận transaction/rowversion hoạt động.

### Integration tests bắt buộc trên SQL Server thật

| ID | Kịch bản | Tiêu chí |
|---|---|---|
| IT01 | Đăng ký/đăng nhập và role | Customer không tạo được Admin; JWT hết hạn bị chặn; route admin trả 403 cho Customer |
| IT02 | Truy cập đơn người khác | GET/cancel trả 404, dữ liệu không đổi |
| IT03 | Checkout thành công | 201; đúng snapshot/tổng; kho giảm; giỏ rỗng |
| IT04 | Giỏ nhiều món, một món thiếu kho | 409; không đơn mới; tất cả tồn kho và giỏ giữ nguyên |
| IT05 | Nhiều khách mua sản phẩm còn 1 | 10 khách có giỏ sẵn, cùng mua 1: đúng một 201, chín 409; tồn cuối 0; đúng một Order mới |
| IT06 | Hủy cùng một đơn đồng thời | Hai request: tổng kho hoàn đúng một lần; request thua có thể 409 hoặc 200 nếu đọc sau commit |
| IT07 | Hủy và đánh dấu Paid cùng lúc | Trạng thái cuối thuộc chuyển hợp lệ; nếu hủy thắng thì kho hoàn một lần, nếu Paid thắng khách không được hủy |
| IT08 | Hai checkout cùng một giỏ | Tối đa một đơn; không trừ kho hai lần; request còn lại 409 hoặc 400 giỏ rỗng |
| IT09 | Sửa giỏ đồng thời checkout | Các kết quả tương đương một thứ tự xử lý hợp lệ hoặc 409; không mất thao tác sửa đã commit |
| IT10 | Admin sửa tồn bằng RowVersion cũ sau checkout | 409, không ghi đè số tồn mới |
| IT11 | Giá/tên sản phẩm đổi sau khi đặt | Đơn cũ giữ giá/tên/tổng cũ |
| IT12 | Lỗi giữa các bước ghi checkout/cancel | Fault injection tại ranh giới lưu: toàn bộ transaction rollback |
| IT13 | Trùng email/danh mục và ràng buộc FK | 409 phù hợp; không trả raw SQL error hoặc tạo dữ liệu mồ côi |
| IT14 | Filter/sort/page | Đúng kết quả và thứ tự ổn định; invalid query trả 400 |
| IT15 | Bổ sung kho lên 1.000.000 rồi hủy đơn cũ | Hoàn đủ số lượng dù tồn cuối vượt giới hạn đầu vào quản trị; không mất khả năng hủy |

Các test đồng thời có fixture cô lập, mỗi request DbContext riêng. Dùng barrier/test hook ở ranh giới sau đọc và trước ghi để tái hiện race có kiểm soát, ngoài thử request song song qua HTTP. Hook chỉ tồn tại trong cấu hình test. Kiểm tra cả response và trạng thái database; log số lượng request, status, tồn trước/sau, số đơn, môi trường và thời điểm chạy. Không đưa số kỳ vọng vào README như kết quả đã đo.

### Definition of Done của MVP

- [ ] Các endpoint MVP hoạt động theo spec; không còn WeatherForecast/test rỗng trong sản phẩm hoàn thành.
- [ ] Có 7 bảng nghiệp vụ, migration, FK, unique/check constraints và concurrency tokens.
- [ ] Kiểm tra role và quyền sở hữu, validation, ProblemDetails thống nhất.
- [ ] Checkout/cancel bảo toàn transaction và vượt qua IT03–IT12 trên SQL Server thật.
- [ ] Ít nhất 10 unit tests hữu ích và integration suite chạy xanh, CI có bằng chứng.
- [ ] Giao diện demo hoàn thành luồng Customer/Admin, Swagger có Authorize.
- [ ] Người mới có thể chạy từ README; Compose và seed kiểm tra được từ database trống.
- [ ] README có sơ đồ dữ liệu/kiến trúc, tính năng, cách chạy/test, ảnh/GIF và kết quả concurrency thực đo.

## 11. Mốc tiếp theo và định hướng thực hiện

Mốc 1: MVP trong spec này. Trình tự gợi ý: nền tảng dữ liệu/auth → catalog → giỏ/đặt hàng → vòng đời/hoàn kho → kiểm thử tích hợp → demo/Compose/README/CI. Test nghiệp vụ đi cùng mỗi phần, không để cuối mới kiểm tra concurrency. Ước lượng 4–5 tuần từ tài liệu nguồn chỉ để tham khảo, phụ thuộc thời gian học/làm mỗi ngày; chưa phải cam kết lịch.

Mốc 2, sau khi MVP đạt DoD:

- **Reviews:** bảng thứ 8 gồm Id, ProductId, UserId, Rating 1–5, Comment tối đa 1.000, CreatedAt; unique(ProductId,UserId). Chỉ Customer có Order Completed chứa sản phẩm mới được đánh giá. Phạm vi API sửa/xóa/moderation cần spec bổ sung trước khi triển khai.
- **Báo cáo:** doanh thu theo tháng và top 5 sản phẩm. Đề xuất tính trên đơn Completed, nhóm theo CompletedAt; đơn Cancelled không tính. Chốt timezone, khoảng ngày và hoàn trả trước khi viết spec mốc 2; không trộn PaidAt với CompletedAt khi thống kê.
- Coupon, xuất CSV, cache, logging nâng cao là backlog riêng, không phải điều kiện MVP.

## 12. Quyết định để người triển khai tự chọn

Người triển khai được chọn tên interface/class/DTO nội bộ, thư viện mock, cách chia fixture, cách trình bày CSS và thư viện Swagger UI tương thích. Không tự thay đổi phạm vi, quyền hủy Paid, quy tắc tính tiền, hợp đồng lỗi, cơ chế bảo vệ tồn kho hoặc bổ sung microservices.

Các quyết định bổ sung cần người dùng chú ý khi duyệt: MVP 7 bảng; admin được hủy Paid nhưng khách chỉ hủy Pending; thanh toán thủ công mô phỏng; VND và phí vận chuyển bằng 0; sản phẩm xóa mềm; Pending không tự hết hạn; giá được tính lại lúc đặt; không refresh token/idempotency key trong MVP; thêm concurrency bảo vệ Order và Cart ngoài Product.

Sau khi người dùng duyệt spec, bước tiếp theo là lập implementation plan bằng Superpowers writing-plans. Việc viết spec này không tự cho phép triển khai mã sản phẩm.
