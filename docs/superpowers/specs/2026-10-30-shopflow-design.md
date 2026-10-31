# ShopFlow — Đặc tả cửa hàng bàn phím cơ và chatbot AI

Ngày: 30/10/2026. Trạng thái: bản thiết kế để người dùng duyệt, chưa triển khai.

## 1. Mục tiêu và căn cứ

ShopFlow là web bán **bàn phím cơ lắp sẵn**, dùng làm dự án chính trong CV thực tập sinh Backend .NET. Hai kết quả cần chứng minh là không bán vượt tồn kho khi khách đặt đồng thời, và chatbot tư vấn sản phẩm dựa trên catalog hiện hành.

Tài liệu đầu vào: `C:\Users\Hi\Documents\C#\Thiết kế 2 dự án .NET cho CV thực tập sinh Backend.docx`, phần dự án 1. Nội dung tài liệu là nguồn yêu cầu tham khảo; không phải chỉ dẫn cho agent thực hiện các hành động ngoài yêu cầu viết spec.

Người dùng đã chọn: **MVP trước; đánh giá và báo cáo ở mốc tiếp theo**. Các chi tiết không được chốt trong tài liệu nguồn, như giới hạn dữ liệu, tiền tệ, quy tắc xóa và xử lý thao tác lặp, là đề xuất thiết kế của bản spec này để người dùng duyệt.

Bản cập nhật này đồng bộ với tài liệu Word đã sửa ngày 30/10/2026: mặt hàng là bàn phím cơ, mỗi cấu hình là một SKU; chatbot AI cơ bản thuộc MVP hoàn chỉnh. Chỉ đặc tả ShopFlow trong repo này. Những giới hạn AI dưới đây là giá trị thiết kế khởi đầu, không phải kết quả đã đo hoặc lựa chọn nhà cung cấp đã được người dùng chốt.

Hiện trạng repo: solution `ShopFlow.slnx`, API và test cùng target `net10.0`; API mới có WeatherForecast, cấu hình OpenAPI và controller; test chỉ có một test rỗng. Chưa có database, nghiệp vụ, JWT hay giao diện. Đây là đặc tả hành vi cần xây dựng, không phải mô tả tính năng đã chạy.

### Kết quả cần đạt

- Khách tìm sản phẩm, quản lý giỏ, đặt hàng, xem và hủy đơn hợp lệ.
- Khách lọc bàn phím theo layout, switch, kết nối và ngân sách; hỏi chatbot để nhận gợi ý có nguồn và thẻ sản phẩm.
- Admin quản lý danh mục, sản phẩm, tồn kho và vòng đời đơn hàng.
- Đặt/hủy đơn bảo toàn dữ liệu khi lỗi hoặc có request đồng thời.
- Có test, API docs, giao diện demo nhỏ và hướng dẫn chạy tái lập được.
- Có kết quả test thực tế để giải thích transaction, optimistic concurrency, LINQ và phân quyền khi phỏng vấn.

### Không thuộc MVP

Đánh giá, báo cáo, coupon, CSV, cache, Serilog nâng cao; thanh toán và hoàn tiền thật; vận chuyển tích hợp; hệ thống biến thể nhiều tầng, tự ráp bàn phím, linh kiện rời và mặt hàng khác; upload ảnh; refresh token, quên mật khẩu, email xác nhận; đa cửa hàng, microservices, Redis, message broker, CQRS/MediatR, Clean Architecture nhiều project. Lịch sử chat lưu dài hạn, RAG/embeddings và vector database để sau khi có nhu cầu thực tế.

## 2. Phương án và quyết định

| Phương án | Đánh đổi | Kết luận |
|---|---|---|
| MVP gồm mua hàng, quản trị và chatbot catalog; đánh giá/báo cáo làm sau | Bản demo có AI hữu ích; vẫn chỉ cần 7 bảng nghiệp vụ | Phù hợp phạm vi người dùng đã yêu cầu trong tài liệu cập nhật |
| Gộp đánh giá và báo cáo vào bản đầu | Đủ 8 bảng như checklist nguồn nhưng tăng thời gian và diện tích kiểm thử | Hoãn đến mốc 2 |
| Chỉ backend API | Nhanh hơn nhưng khó trình diễn cho người không dùng Swagger | Không chọn làm phạm vi giao hàng; có thể phát triển API trước |

Giữ kiến trúc Controller–Service–Repository của tài liệu nguồn trong một project API. So với Clean Architecture nhiều project, cách này ít lớp trung gian hơn và đủ để trình bày phân tách trách nhiệm. So với đặt toàn bộ logic trong controller, service và entity giúp kiểm thử nghiệp vụ độc lập. Đây là đề xuất kỹ thuật dựa trên nguồn và repo hiện có.

## 3. Công nghệ và cấu trúc

| Thành phần | Quyết định |
|---|---|
| API | ASP.NET Core Web API .NET 10, controller, async/await, DI |
| Dữ liệu | EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL và PostgreSQL; migrations quản lý schema |
| Xác thực | JWT Bearer; PasswordHasher của ASP.NET Core để hash/verify mật khẩu |
| Tài liệu API | OpenAPI và Swagger UI có Bearer Authorize |
| Test | xUnit hiện có; chọn một trong Moq/NSubstitute khi cần mock |
| Test tích hợp | API test host và PostgreSQL thật trong Docker |
| Demo | React gọi API; build thành file tĩnh phục vụ từ wwwroot cùng origin với API |
| AI | LLM qua adapter server ILlmClient; truy vấn catalog có cấu trúc, tool chỉ đọc; cấu hình provider/model khi lập kế hoạch |
| Chạy và CI | Docker Compose API + PostgreSQL; GitHub Actions build/test |

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
  AI/                # IChatAssistant, ILlmClient, tool catalog, prompt và adapter
  Content/           # Chính sách cửa hàng có version, seed cùng ứng dụng
  wwwroot/           # Tài sản demo từ web khi build/publish
  Program.cs
tests/ShopFlow.Tests/ # Unit tests hiện có, thay test rỗng
tests/ShopFlow.IntegrationTests/
web/                 # Mã nguồn frontend React
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

Luồng tư vấn: Customer đăng nhập → nêu ngân sách/layout/nhu cầu → chatbot hỏi rõ hoặc tra catalog → hiển thị tối đa 5 thẻ bàn phím → khách mở chi tiết và tự thêm giỏ. Admin không dùng endpoint chat chỉ dành cho Customer.

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

- Chỉ bán bàn phím cơ lắp sẵn. Danh mục là nhóm layout: `65%`, `75%`, `TKL`, `Full-size`; nhóm mới do Admin tạo. Layout trong response chính là Category.Name, không có cột Layout độc lập để tránh lệch dữ liệu.
- Một tổ hợp model, layout, switch, màu và kết nối là một SKU có kho riêng. Sku trim, chuyển chữ hoa ASCII, 3–50 ký tự thuộc `[A-Z0-9-]`, unique; ví dụ `SF75-BLK-LIN`. Không đổi SKU, CategoryId, Brand, SwitchType, Connectivity, Color hoặc IsHotSwappable sau khi có OrderItem tham chiếu: trả 409 PRODUCT_CONFIGURATION_LOCKED; tạo SKU khác nếu muốn bán cấu hình khác. Name/Description/Price/Stock/IsActive vẫn sửa được với RowVersion.
- Brand trim, 1–100 ký tự; Color trim, 1–50; SwitchType thuộc `Linear`, `Tactile`, `Clicky`, `Unknown`; Connectivity là cờ `Wired`, `Bluetooth`, `Wireless2_4G`, có ít nhất một cờ; IsHotSwappable là bool nullable, null nghĩa chưa có thông tin. Unknown/null phải hiển thị rõ, chatbot không đoán. Tên switch cụ thể hoặc thông tin tiếng ồn chỉ diễn giải khi Description có dữ liệu xác minh.
- Tên danh mục trim, 1–100 ký tự; NormalizedName unique. Danh mục có sản phẩm tham chiếu, kể cả sản phẩm ngừng bán, không được xóa: 409.
- Category.Name là định danh layout: không cho đổi Name/NormalizedName khi danh mục đã có sản phẩm, trả 409 CATEGORY_CONFIGURATION_LOCKED; muốn layout khác thì tạo danh mục khác. Quy tắc này tránh đổi cấu hình SKU gián tiếp bằng cách đổi tên danh mục. Chỉ danh mục trống mới được đổi tên; kiểm tra và cập nhật trong transaction Serializable để cạnh tranh với thao tác thêm/chuyển sản phẩm không vượt qua quy tắc.
- Tên sản phẩm 1–200 ký tự, mô tả tối đa 2.000 ký tự; CategoryId phải tồn tại.
- Tiền tệ duy nhất VND; dùng decimal và `numeric(18,2)`, chỉ nhận số tiền nguyên VND trong MVP. Giá từ 1 đến 1.000.000.000; Stock nhập qua API quản trị là số nguyên từ 0 đến 1.000.000. Giới hạn 1.000.000 chỉ áp dụng đầu vào quản trị, không áp dụng kết quả hoàn kho: tồn sau hủy có thể cao hơn nếu admin đã bổ sung hàng. Invariant lưu trữ là Stock không âm; dùng bigint/C# long và phép toán checked, lỗi vượt miền kiểu số phải rollback toàn bộ.
- DELETE sản phẩm là ngừng bán (`IsActive=false`), không xóa vật lý; admin có thể kích hoạt lại bằng PUT. Đơn cũ vẫn đọc được.
- Khách chỉ thấy sản phẩm active; sản phẩm hết hàng vẫn hiển thị với Stock=0. Chi tiết sản phẩm inactive trả 404 cho khách; admin có endpoint đọc riêng.
- PUT sản phẩm cập nhật cả thông tin và số tồn tuyệt đối, yêu cầu RowVersion đã đọc gần nhất. Bản sửa cũ không được ghi đè tồn kho sau một lần bán: 409.
- GET danh sách: `q` tối đa 100 ký tự, tìm Contains theo Name hoặc Sku; `categoryId`, `switchType`, `connectivity`, `minPrice`, `maxPrice`; connectivity lọc một giá trị và khớp sản phẩm có hỗ trợ kiểu kết nối đó. Enum không hợp lệ trả 400. `sort` thuộc `newest`, `priceAsc`, `priceDesc`, `nameAsc`. Mặc định newest; thêm Id làm khóa phụ ổn định.
- `page=1`, `size=20` mặc định; page >=1, size 1–100. Khoảng giá không hợp lệ hoặc sort không hỗ trợ trả 400; trang vượt phạm vi trả items rỗng.
- Quy tắc so khớp chữ/dấu theo collation PostgreSQL được ghi trong README; không cam kết tìm kiếm bỏ dấu.

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
- OrderItem lưu Sku, ProductName và UnitPrice tại lúc checkout. Tổng tiền bằng tổng `UnitPrice × Quantity`, backend tự tính; chưa có thuế/phí vận chuyển/giảm giá.
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

### 5.6 Chatbot tư vấn bàn phím

Chatbot là phần bắt buộc của bản demo MVP hoàn chỉnh. Ví dụ: “Mình cần bàn phím 75% dưới 1,5 triệu để gõ văn bản”. Bot chỉ so sánh dữ liệu đã có, không tự khẳng định switch linear luôn êm hoặc model nào bền hơn khi chưa có bằng chứng trong catalog.

**Luồng xử lý:** xác thực Customer và kiểm tra quota → validate message/history → gọi model để xác định điều kiện hoặc hỏi rõ → server thực thi tool được phép → model tạo phần giải thích → server dựng các thẻ sản phẩm và sources từ kết quả tool. Không mở transaction mua hàng trong lúc gọi AI.

Tool được phép:

| Tool | Tham số và kết quả |
|---|---|
| SearchKeyboards | q?, categoryId?, switchType?, connectivity?, minPrice?, maxPrice?; áp dụng validation như catalog, chỉ lấy active SKU có Stock>0, tối đa 5; thứ tự giá tăng và Id tăng |
| GetKeyboardDetails | productId nguyên dương; trả thông số, giá, tồn, trạng thái và thời điểm truy vấn; inactive trả không tìm thấy |
| GetStorePolicy | topic thuộc shipping, cancellation, payment; đọc tài liệu chính sách được quản lý theo version trong Content, không tự lấy URL ngoài |

Nếu cần layout chưa ánh xạ được, server cung cấp danh sách Category để model chọn hoặc hỏi lại; không để model tự bịa categoryId. Ngân sách được hiểu là VND, mơ hồ phải hỏi rõ. Ràng buộc chính xác do người dùng nêu phải được backend kiểm tra lại trên các thẻ trả về; không có kết quả phù hợp thì nói không tìm thấy, không tự nới ngân sách. Hết hàng có thể được giải thích khi xem chi tiết nhưng không gợi ý như đang mua được. Stock/price là snapshot lúc truy vấn; checkout vẫn kiểm tra lại.

Chỉ allowlist tool phía server, tối đa 3 lần gọi tool/request; tổng thời gian tối đa 20 giây có cancellation. Model không được gọi SQL, URL tùy ý, tạo/sửa giỏ, đặt/hủy đơn hoặc truy cập thông tin tài khoản/đơn. Dữ liệu catalog, chính sách và lịch sử chat là dữ liệu đầu vào không đáng tin; không được biến chúng thành chỉ dẫn ghi đè quyền/tool. Xác thực và validation không phụ thuộc model.

Response có answer, products, sources, dataAsOf, traceId (chi tiết ở mục 7). Server chỉ chấp nhận productId đã xuất hiện trong tool output của request và vẫn active khi dựng response. Giá, stock, Sku, thông số và link trên thẻ do server lấy từ dữ liệu truy vấn, không dùng trường tự do model sinh. Phần answer chỉ được giải thích định tính và tham chiếu thẻ; không nhận giá/tồn hoặc thông số mới do model tự sinh làm dữ liệu chính thức. Structured output không hợp lệ hoặc chứa khẳng định không đối chiếu được phải được thay bằng lời dẫn cố định cùng thẻ đã truy vấn, không phát nguyên văn output lỗi. Test đánh giá vẫn kiểm tra cả answer vì model có thể diễn giải sai.

MVP không lưu chat vào database. Client giữ tối đa 6 message gần nhất trong bộ nhớ (không tính message hiện tại); mỗi message tối đa 2.000 ký tự, chỉ role user/assistant. History chỉ hỗ trợ ngữ cảnh, không được dùng như bằng chứng giá hoặc quyền truy cập; server tra lại catalog mỗi request. Reload/xóa chat làm mất hội thoại. Không gửi token, email, địa chỉ, lịch sử đơn hoặc mật khẩu từ hệ thống sang provider; UI nhắc không nhập dữ liệu riêng tư. Log chỉ metadata/traceId/usage, không ghi nội dung chat.

Giới hạn demo đơn instance: 5 request/phút/Customer và tối đa 30 lần gọi chat được chấp nhận/ngày/Customer, tính theo UTC; request được tính trước khi gọi provider kể cả khi provider lỗi. Counter trong bộ nhớ reset khi restart, giới hạn này là biện pháp demo, không phải sổ tính phí bền vững. Provider có trần chi tiêu cấu hình riêng; hết quota trả 429. Timeout trả 504, provider lỗi hoặc thiếu cấu hình trả 503, không tự retry request tốn phí. Mock chỉ dành cho test hoặc chế độ demo ghi rõ; DoD cần kiểm chứng ít nhất một provider thật.

Chính sách seed nêu đúng nghiệp vụ: phí vận chuyển 0 trong demo, chưa tích hợp hãng vận chuyển; khách hủy Pending; Paid xác nhận thủ công. Tài liệu chính sách luôn có version và endpoint đọc công khai để sources có đích truy cập.

## 6. Dữ liệu và ràng buộc

ID dùng integer GENERATED BY DEFAULT AS IDENTITY; timestamp dùng timestamptz, C# DateTimeOffset luôn có offset 0 (UTC); role/status lưu chuỗi có giới hạn và check constraint. Mọi trường mặc định NOT NULL trừ các timestamp chưa xảy ra. RowVersion là thuộc tính C# uint ánh xạ cột hệ thống PostgreSQL xmin bằng Npgsql IsRowVersion(); không tạo cột rowversion riêng. DTO admin truyền rowVersion dưới dạng số uint; version nguồn AI là chuỗi thập phân của giá trị này. Token chỉ dùng kiểm tra xung đột, không phải thời gian hay số phiên bản tăng mãi.

| Bảng | Trường và ràng buộc chính |
|---|---|
| Users | Id, Email varchar(254), NormalizedEmail varchar(254) unique, PasswordHash varchar(512), FullName varchar(100), Role varchar(20), CreatedAt |
| Categories | Id, Name varchar(100), NormalizedName varchar(100) unique |
| Products | Id, CategoryId FK, Sku varchar(50) unique, Name varchar(200), Brand varchar(100), SwitchType varchar(20), Connectivity int (bit flags), Color varchar(50), IsHotSwappable boolean nullable, Description varchar(2000), Price numeric(18,2), Stock bigint, IsActive boolean, CreatedAt, RowVersion; check Price>0, Stock>=0, Connectivity từ 1 đến 7, SwitchType thuộc enum |
| Carts | Id, UserId FK unique, Revision bigint mặc định 0, cấu hình concurrency token |
| CartItems | Id, CartId FK, ProductId FK, Quantity int; unique(CartId,ProductId); check Quantity từ 1 đến 99 |
| Orders | Id, UserId FK, Status varchar(20), TotalAmount numeric(18,2), ShippingAddress varchar(500), CreatedAt, PaidAt?, ShippedAt?, CompletedAt?, CancelledAt?, RowVersion; check TotalAmount>0 |
| OrderItems | Id, OrderId FK, ProductId FK, Sku varchar(50), ProductName varchar(200), UnitPrice numeric(18,2), Quantity int; unique(OrderId,ProductId); check UnitPrice>0, Quantity từ 1 đến 99 |

Products.Description mặc định chuỗi rỗng. Index: Products(CategoryId), Products(Name), Orders(UserId,CreatedAt), Orders(Status,CreatedAt), OrderItems(ProductId). Không hứa index Name sẽ tối ưu được Contains; chỉ đo và điều chỉnh khi có nhu cầu thực tế.

Quan hệ: User 1–1 Cart; Cart 1–n CartItem; Category 1–n Product; User 1–n Order; Order 1–n OrderItem; Product 1–n CartItem và OrderItem. Không cascade xóa Product/User vào lịch sử đơn. Không có endpoint xóa user/order trong MVP. CartItem có thể cascade khi xóa Cart; OrderItem được cấu hình restrict để tránh mất lịch sử do thao tác ngoài dự kiến.

MVP có **7 bảng nghiệp vụ**; chính sách và prompt là nội dung version cùng mã nguồn, chat không lưu dài hạn nên không thêm bảng chat. Reviews là bảng thứ 8 ở mốc 2. Products.IsHotSwappable là ngoại lệ nullable ngoài các timestamp đã nêu. Không tính bảng migration vào số bảng nghiệp vụ.

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
| POST | /chat/messages | Customer | 200: answer, products, sources, dataAsOf, traceId |
| GET | /store-policies/{topic} | Public | 200: topic, version, content; topic không tồn tại trả 404 |

Danh sách đơn sắp CreatedAt giảm dần, Id giảm dần; dùng page/size và giới hạn như sản phẩm. Status không thuộc enum trả 400. Mọi route quản trị thiếu token trả 401, sai role trả 403. Kiểm tra quyền sở hữu trước khi tiết lộ chi tiết đơn.

Product create/update body: `categoryId`, `sku`, `name`, `brand`, `switchType`, `connectivity` (mảng giá trị enum không trùng), `color`, `isHotSwappable`, `description`, `price`, `stock`, `isActive`; update thêm `rowVersion`. Server chuyển connectivity sang bit flags trong database; DTO luôn là mảng chuỗi enum. Category body: `name`. Register body: `email`, `password`, `fullName`; login: `email`, `password`.

DTO sản phẩm công khai gồm id, categoryId, layout (Category.Name), sku, name, brand, switchType, connectivity, color, isHotSwappable, description, price, stock, isActive, createdAt; DTO admin thêm rowVersion. Giỏ và OrderItem response thêm `sku`; snapshot SKU trong đơn không đọc lại từ catalog.

Chat request: `{message, history:[{role,content}]}` với history tùy chọn theo giới hạn mục 5.6. Role khác user/assistant, history quá dài hoặc message trắng trả 400. Response: `{answer, products:[{id,sku,name,layout,brand,switchType,connectivity,color,isHotSwappable,price,stock,url}], sources:[{kind,id,version,url}], dataAsOf,traceId}`. dataAsOf là thời gian UTC truy vấn database, không phải cam kết giữ giá. Product source có kind=product, id là ProductId dạng chuỗi, version là RowVersion dạng chuỗi thập phân; policy source có kind=policy, id là topic và version nội dung. Link sản phẩm dùng trang chi tiết demo `/products.html?id={id}`; link policy dùng `/api/store-policies/{topic}`, đều do server dựng. Không có kết quả trả 200 với products rỗng và lời hỏi rõ/không tìm thấy, không coi là lỗi hạ tầng.

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

Token response: `{accessToken, tokenType:"Bearer", expiresAt, user:{id,email,fullName,role}}`. Giỏ: `{id, items:[{productId,sku,productName,unitPrice,quantity,lineTotal,stock,isActive,isAvailable}], totalAmount}`. Đơn chi tiết: `{id,status,totalAmount,shippingAddress,createdAt,paidAt,shippedAt,completedAt,cancelledAt,items:[{productId,sku,productName,unitPrice,quantity,lineTotal}]}`. Danh sách đơn trả các trường tóm tắt, không nhúng toàn bộ items. UserId được thêm vào DTO quản trị đơn.

### Lỗi

Lỗi dùng ProblemDetails: `type`, `title`, `status`, `detail`, `instance`, thêm `code` ổn định và `traceId`; validation thêm `errors` theo field. Type mặc định `about:blank`. Không đưa SQL, stack trace, token hoặc password vào response/log.

| HTTP | Trường hợp / code mẫu |
|---|---|
| 400 | VALIDATION_ERROR, EMPTY_CART, status/query không hợp lệ |
| 401 | Token thiếu/hỏng/hết hạn; INVALID_CREDENTIALS |
| 403 | FORBIDDEN: đúng danh tính nhưng sai role |
| 404 | NOT_FOUND: tài nguyên không có hoặc đơn không thuộc khách |
| 409 | EMAIL_EXISTS, CATEGORY_EXISTS, SKU_EXISTS, PRODUCT_CONFIGURATION_LOCKED, CATEGORY_CONFIGURATION_LOCKED, CATEGORY_IN_USE, INSUFFICIENT_STOCK, PRODUCT_UNAVAILABLE, CONCURRENCY_CONFLICT, INVALID_ORDER_TRANSITION, CART_LIMIT_EXCEEDED |
| 429 | RATE_LIMITED ở endpoint đăng ký/đăng nhập/chat hoặc AI_QUOTA_EXCEEDED |
| 503 | AI_UNAVAILABLE: provider lỗi hoặc chưa cấu hình; Retry-After nếu xác định được |
| 504 | AI_TIMEOUT: vượt thời gian xử lý chat |
| 500 | INTERNAL_ERROR: lỗi chưa dự kiến, log với traceId |

Áp dụng cùng định dạng cho lỗi validation tự động và auth challenge/forbid. Unique violation khi đăng ký/tạo danh mục đồng thời được ánh xạ đúng 409. Hai thao tác thêm cùng một sản phẩm vào giỏ có thể chạm unique(CartId,ProductId) trước khi phát hiện Revision; ánh xạ đúng constraint này thành 409 CONCURRENCY_CONFLICT và rollback toàn bộ. Không coi mọi DbUpdateException là lỗi trùng hoặc lỗi concurrency.

## 8. Đồng thời và tính nguyên tử

Ba phạm vi bảo vệ khác nhau:

1. **Products.RowVersion** bảo vệ số tồn và giá/thông tin được đọc trong checkout; version đổi trước khi ghi làm thao tác thất bại.
2. **Orders.RowVersion** bảo vệ trạng thái, đặc biệt hủy đồng thời với hủy hoặc Paid.
3. **Carts.Revision** bảo vệ toàn bộ tập dòng giỏ; mọi ghi CartItems phải đi kèm tăng Revision trong cùng transaction. Khi checkout đọc giỏ phải đọc parent Revision trước các dòng; request dùng context riêng và không tái sử dụng snapshot cũ.

Thiết kế dùng optimistic concurrency và transaction ngắn ở mức ReadCommitted mặc định; không dùng lock trong bộ nhớ làm bảo đảm đúng. Quy tắc token và xử lý DbUpdateConcurrencyException dựa trên [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).

Tất cả thay đổi của một use case checkout/cancel là một transaction. Trên conflict, rollback toàn bộ transaction và kết thúc request, không tiếp tục lưu bằng change tracker đã lỗi. Không bật retry tự động cho luồng này trong MVP; nếu sau này dùng execution strategy, phải thiết kế retry cho toàn bộ đơn vị transaction. Căn cứ cơ chế atomicity và giới hạn execution strategy: [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

Giới hạn: optimistic concurrency có thể từ chối một thao tác dù vẫn còn hàng; chấp nhận để client đọc lại. Tránh gọi dịch vụ ngoài hoặc render giao diện trong transaction. Không dùng SQLite hoặc EF InMemory làm bằng chứng PostgreSQL xmin hoạt động đúng.

## 9. Giao diện demo và vận hành

MVP có các màn hình tối thiểu: đăng ký/đăng nhập; catalog và chi tiết bàn phím với bộ lọc layout/switch/kết nối; giỏ và địa chỉ checkout; danh sách/chi tiết đơn; admin danh mục/SKU; admin danh sách/chi tiết và trạng thái đơn; khung chat tư vấn với thẻ sản phẩm, nguồn và thời điểm dữ liệu. Frontend React mở trang chi tiết từ liên kết sản phẩm trong thẻ AI; đường dẫn cụ thể được chốt khi triển khai giao diện. Khung chat có loading, xóa hội thoại, cảnh báo giới hạn quota và lỗi/timeout; chỉ hiển thị cho Customer. Không render HTML model sinh; hiển thị dữ liệu qua text được React escape và link server đã kiểm tra, không dùng dangerouslySetInnerHTML cho dữ liệu này.

- Ẩn thao tác không phù hợp role/trạng thái; backend vẫn kiểm tra độc lập.
- Chặn bấm submit liên tục để cải thiện UX; không coi đây là biện pháp chống trùng dữ liệu.
- Khi 409, hiển thị lỗi dễ hiểu và tải lại dữ liệu; không tự gửi lại checkout. Khi 401, yêu cầu đăng nhập lại.
- Hiển thị giá VND, trạng thái rỗng/đang tải/lỗi; hiển thị dữ liệu người dùng qua text được React escape để tránh chèn HTML.
- Demo cùng origin giúp không cần CORS rộng; nếu chạy web riêng, chỉ cho phép origin cấu hình.

Docker Compose có API và PostgreSQL với volume dữ liệu, healthcheck và cấu hình môi trường. API phục vụ frontend React đã build trong wwwroot và Swagger UI. Mã nguồn FE nằm riêng tại web/; không yêu cầu container FE riêng. Khi phát triển, frontend có thể dùng dev server riêng với proxy API. Seed có danh mục, sản phẩm mẫu, một admin cấu hình từ môi trường; chạy seed nhiều lần không nhân bản. Migration tự động chỉ cho local/demo đơn instance; môi trường triển khai khác dùng bước migration riêng được mô tả.

Seed 12–20 SKU bàn phím với 4 nhóm layout, nhiều switch/kết nối/mức giá, ít nhất một SKU hết hàng và một SKU inactive để kiểm tra. Giá mẫu gắn nhãn minh họa, thông số Unknown/null hiện đúng trạng thái. Seed idempotent theo SKU và NormalizedName, không ghi đè tồn/giá của dữ liệu đã được người dùng sửa.

Cấu hình AI qua biến môi trường: provider, model, API key, timeout, giới hạn output/token và quota. Không commit key; không chốt tên model hoặc chi phí chưa kiểm tra lúc triển khai. Thiếu key chỉ làm chat trả 503, không làm API catalog/đơn hàng hoặc `/health` mất sẵn sàng. README mô tả rõ chế độ fake và live. Theo dõi latency, timeout, usage nếu provider trả về và phiên bản prompt; CI mặc định dùng fake, không tiêu tiền API thật.

README cung cấp `.env.example` không chứa secret thật, yêu cầu môi trường, lệnh chuẩn bị và `docker compose up --build`, cách reset dữ liệu có cảnh báo rõ. PostgreSQL healthy trước bước migration/seed. Container local có thể dùng HTTP qua loopback được cấu hình rõ; triển khai công khai phải có HTTPS và secret riêng.

Health endpoint `/health` trả 200 khi API/database sẵn sàng, 503 khi database không dùng được; không lộ connection string. Đây là kiểm tra sẵn sàng tối thiểu để chạy Compose, khác bộ health/observability nâng cao để sau.

Đăng nhập/đăng ký giới hạn mặc định 10 request/phút/IP trong demo đơn instance; 429 có Retry-After. Log request lỗi có traceId, thời gian và loại lỗi, không log body đăng nhập/địa chỉ đầy đủ. Cấu hình JWT validation theo [hướng dẫn ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

CI build và chạy unit tests mỗi push/PR; integration tests dùng PostgreSQL service/container riêng, không dùng database thật của người dùng. Không đặt chỉ tiêu latency/throughput chưa đo; tối ưu sau khi có số liệu. Mọi endpoint danh sách lớn phải phân trang.

## 10. Kiểm thử và tiêu chí nghiệm thu

### Unit tests bắt buộc

Ít nhất 10 test có ý nghĩa, bao phủ: tính tổng nhiều dòng; giữ snapshot giá/tên; thiếu kho; số lượng không hợp lệ; từng chuyển trạng thái hợp lệ; chuyển sai; khách hủy Paid bị từ chối; hủy hoàn kho; lặp hủy không hoàn thêm; phân biệt quyền sở hữu. Tách unit test entity và service; không dùng mock để kết luận transaction/rowversion hoạt động.

### Integration tests bắt buộc trên PostgreSQL thật

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
| IT16 | Catalog bàn phím | SKU chuẩn hóa unique; filter layout/switch/kết nối đúng; enum sai trả 400; cấu hình SKU có đơn bị khóa 409; đổi tên danh mục có sản phẩm bị chặn cả khi thêm sản phẩm đồng thời |
| IT17 | Snapshot SKU | Thay tên/giá catalog không đổi tên/giá/SKU của đơn đã tạo |
| IT18 | Chat dùng fake provider, database thật | Tool áp dụng đúng ngân sách/layout; tối đa 5 active SKU; productId bịa bị loại; giá/tồn/thông số thẻ và sources đúng database |
| IT19 | Chat lỗi/giới hạn | Role, history và tool không hợp lệ bị chặn; 429/503/504 đúng contract; catalog và checkout vẫn hoạt động khi AI hỏng |
| IT20 | AI chống vượt quyền và sai dữ liệu | Injection trong history/catalog không mở tool ngoài allowlist; không truy cập đơn hoặc ghi dữ liệu; output sai cấu trúc/không có căn cứ dùng fallback an toàn |

Thêm unit test validation SKU/thông số, ánh xạ connectivity, allowlist tool, giới hạn vòng gọi, dựng source/link và fallback AI. Bộ đánh giá riêng có ít nhất 20 câu hỏi chạy provider thật: ngân sách, so sánh layout/switch, kết nối nhiều chế độ, hết hàng, inactive, không có kết quả, thiếu thông số, câu hỏi ngoài phạm vi, injection, yêu cầu tạo/hủy đơn và lỗi provider. Ghi input, dữ liệu kỳ vọng, model/prompt version, ngày chạy, kết quả từng tiêu chí và độ trễ; không đưa nội dung riêng tư vào fixture. Tiêu chí bắt buộc: mọi thẻ khớp catalog/điều kiện, không lộ dữ liệu hoặc ghi nghiệp vụ, không bịa thông số; lỗi phát hiện phải sửa hoặc dùng fallback trước nghiệm thu. Báo cáo riêng chất lượng diễn đạt, không dùng tỷ lệ trung bình để bỏ qua lỗi quyền hoặc giá.

Các test đồng thời có fixture cô lập, mỗi request DbContext riêng. Dùng barrier/test hook ở ranh giới sau đọc và trước ghi để tái hiện race có kiểm soát, ngoài thử request song song qua HTTP. Hook chỉ tồn tại trong cấu hình test. Kiểm tra cả response và trạng thái database; log số lượng request, status, tồn trước/sau, số đơn, môi trường và thời điểm chạy. Không đưa số kỳ vọng vào README như kết quả đã đo.

### Definition of Done của MVP

- [ ] Các endpoint MVP hoạt động theo spec; không còn WeatherForecast/test rỗng trong sản phẩm hoàn thành.
- [ ] Có 7 bảng nghiệp vụ, migration, FK, unique/check constraints và concurrency tokens.
- [ ] Kiểm tra role và quyền sở hữu, validation, ProblemDetails thống nhất.
- [ ] Checkout/cancel bảo toàn transaction và vượt qua IT03–IT12 trên PostgreSQL thật.
- [ ] Ít nhất 10 unit tests hữu ích và integration suite chạy xanh, CI có bằng chứng.
- [ ] Giao diện demo hoàn thành luồng Customer/Admin, Swagger có Authorize.
- [ ] Catalog/seed và API có SKU/thông số bàn phím; snapshot đơn có SKU; IT16–IT20 chạy xanh với fake provider.
- [ ] Chatbot tích hợp ít nhất một provider AI thật, có sources/thẻ lấy từ catalog; bộ 20 câu hỏi có kết quả và đáp ứng tiêu chí an toàn dữ liệu/đúng sản phẩm ở trên.
- [ ] Chat có quota, timeout, fallback và cấu hình secret; AI lỗi không chặn luồng mua hàng.
- [ ] Người mới có thể chạy từ README; Compose và seed kiểm tra được từ database trống.
- [ ] README có sơ đồ dữ liệu/kiến trúc, tính năng, cách chạy/test, ảnh/GIF và kết quả concurrency thực đo.

## 11. Mốc tiếp theo và định hướng thực hiện

Mốc 1: MVP trong spec này. Trình tự gợi ý: dữ liệu/auth → catalog bàn phím → giỏ/checkout/vòng đời/hoàn kho → web demo và test tích hợp → chatbot catalog và đánh giá → Compose/README/CI. Test đi cùng mỗi phần. Ước lượng học tập cập nhật 5–6 tuần bao gồm AI cơ bản, phụ thuộc thời gian học/làm và quyền truy cập provider; chưa phải cam kết lịch. Có thể demo lõi trước, nhưng chỉ gọi MVP hoàn chỉnh khi chatbot đạt DoD.

Mốc 2, sau khi MVP đạt DoD:

- **Reviews:** bảng thứ 8 gồm Id, ProductId, UserId, Rating 1–5, Comment tối đa 1.000, CreatedAt; unique(ProductId,UserId). Chỉ Customer có Order Completed chứa sản phẩm mới được đánh giá. Phạm vi API sửa/xóa/moderation cần spec bổ sung trước khi triển khai.
- **Báo cáo:** doanh thu theo tháng và top 5 sản phẩm. Đề xuất tính trên đơn Completed, nhóm theo CompletedAt; đơn Cancelled không tính. Chốt timezone, khoảng ngày và hoàn trả trước khi viết spec mốc 2; không trộn PaidAt với CompletedAt khi thống kê.
- Coupon, xuất CSV, cache, logging nâng cao là backlog riêng, không phải điều kiện MVP.
- Tư vấn từ kho hướng dẫn dài bằng RAG/embeddings, lịch sử hội thoại bền vững, linh kiện rời và bộ cấu hình bàn phím là các mở rộng cần spec riêng.

## 12. Quyết định để người triển khai tự chọn

Người triển khai được chọn tên interface/class/DTO nội bộ, thư viện mock, cách chia fixture, cách trình bày CSS và thư viện Swagger UI tương thích. Không tự thay đổi phạm vi, quyền hủy Paid, quy tắc tính tiền, hợp đồng lỗi, cơ chế bảo vệ tồn kho hoặc bổ sung microservices.

Provider/model/SDK AI được chọn lúc lập kế hoạch sau khi kiểm tra khả năng structured output hoặc tool calling, điều khoản dữ liệu, chi phí và khả năng truy cập của người dùng. Giữ ILlmClient để thay provider. Mọi công cụ ghi nghiệp vụ, thêm bảng chat hoặc đổi quota thành hệ thống tính phí thật đều cần cập nhật thiết kế trước.

Các quyết định bổ sung cần người dùng chú ý khi duyệt: MVP 7 bảng; admin được hủy Paid nhưng khách chỉ hủy Pending; thanh toán thủ công mô phỏng; VND và phí vận chuyển bằng 0; sản phẩm xóa mềm; Pending không tự hết hạn; giá được tính lại lúc đặt; không refresh token/idempotency key trong MVP; thêm concurrency bảo vệ Order và Cart ngoài Product.

Sau khi người dùng duyệt spec, bước tiếp theo là lập implementation plan bằng Superpowers writing-plans. Việc viết spec này không tự cho phép triển khai mã sản phẩm.

Ghi chú chuyển PostgreSQL theo lựa chọn người dùng: Docker dùng PostgreSQL 17 ở bước học hiện tại. Với Npgsql/EF Core 10, xử lý DbUpdateConcurrencyException và lỗi PostgreSQL 40001/40P01 (kể cả tại commit) bằng rollback và trả 409 cho thao tác xung đột, không tự retry checkout. Kiểm chứng lại các transaction Serializable bằng integration test PostgreSQL thật. Tham khảo mapping xmin: https://www.npgsql.org/efcore/modeling/concurrency.html
