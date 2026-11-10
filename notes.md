# ShopFlow — Tiến độ và bàn giao

Cập nhật nội dung tiến độ: 10/11/2026 (Asia/Saigon).

Quy tắc làm việc đã được tách sang [AGENTS.md](AGENTS.md) ngày 10/11/2026. File này giữ tiến độ, bằng chứng và lịch sử bàn giao; các hướng dẫn theo phiên cũ bên dưới chỉ là bối cảnh lịch sử.

## Kết phiên 10/11 — mới nhất: bộ lọc và PUT đã có, tiếp theo ngừng bán sản phẩm

- Đã đọc code đã lưu: ProductQueryRequest và bộ lọc public Q/categoryId/giá/switch/connectivity/sort; PUT Admin nối UpdateProductRequest -> UpdateAsync -> repository tracking, SKU loại trừ ID, OriginalRowVersion, lưu/map. DTO implement IValidatableObject, RowVersion nullable/Required. Repository chuyển DbUpdateConcurrencyException thành ProductConcurrencyException; controller trả409/CONCURRENCY_CONFLICT. SetOriginalRowVersion buộc UPDATE bằng Name IsModified kể cả body không đổi.
- Sau hướng dẫn body PUT và gửi lại phiên bản cũ, người dùng nói “test được rồi”. Ghi nhận xác nhận chung của người dùng; chưa có response/ảnh riêng xác nhận cả200 và409, không coi mọi case đã pass. Các case PUT thiếuID, SKU trùng, danh mục thiếu, validation, đổi danh mục, inactive/kích hoạt lại và concurrency độc lập chưa được kiểm chứng đầy đủ.
- Catalog: người dùng xác nhận bộ case Q/categoryId đã giao đúng; giá/sort có xác nhận chung. Switch/connectivity và các biên chưa có xác nhận riêng. Không chạy lại unit/integration hoặc HTTP trong lượt kết phiên; kết quả test auth/category trước đây là lịch sử.
- Build cuối phiên mới: dotnet build ShopFlow.slnx --no-restore ngoài sandbox thành công 0 warning/0 error. Không sửa backend, migration hoặc commit trong lượt kết phiên.
- Đã cập nhật nội dung learning-plan buổi12–14, giữ30 buổi/120 ID. Buổi12 học/thực hành đã có đủ nội dung, kiểm chứng còn dở; buổi14 đã học RowVersion và có PUT, thực hành còn DELETE/khóa cấu hình khi có OrderItem. Không tick trọn buổi14 chỉ vì PUT đã có.
- Đã cập nhật và đọc lại PersonalProgress: day-12-learn/build và day-14-learn/review được tick; day-12-verify, day-14-build/verify vẫn false. Tổng48/120 mục (40%). Đối chiếu giữ nguyên trạng thái/timestamp các mục khác; web đọc được nội dung kế hoạch mới, không rebuild tracker hoặc thay volume.
- Phiên sau: hướng dẫn DELETE /api/admin/products/{id} để ngừng bán bằng IsActive=false, không xóa vật lý; thống nhất cách dùng RowVersion cho thao tác này theo spec trước khi viết. Sau đó kiểm tra public không thấy/inactive chi tiết404, Admin vẫn đọc, PUT kích hoạt lại. Người dùng tự viết mặc định; không mở rộng quyền viết hộ các bước trước sang bước mới. Khóa cấu hình sau OrderItem sẽ bổ sung khi có schema đơn hàng.
- Còn hợp đồng DTO public trong spec ghi isActive/createdAt nhưng code public bỏ hai trường; cần thống nhất trước khi chốt API. Namespace UpdateProductRequest hiện ShopFlow.Api.Products, khác các DTO khác; không tự refactor khi kết phiên.

## Cập nhật 10/11 — đã nối PUT sản phẩm, chờ kiểm thử HTTP

- Theo yêu cầu làm phần5, trợ lý thêm ProductConcurrencyException; SaveChangAsync bắt DbUpdateConcurrencyException trước DbUpdateException và chuyển thành lỗi nghiệp vụ. AdminProductsController có PUT /api/admin/products/{id:int}, kế thừa quyền Admin: thành công200, thiếu404, SKU trùng409/SKU_EXISTS, xung đột409/CONCURRENCY_CONFLICT, danh mục thiếu400/CATEGORY_NOT_FOUND, input sai400.
- SetOriginalRowVersion đánh dấu Name IsModified để PUT luôn thực thi UPDATE kiểm tra phiên bản tại database ngay cả khi dữ liệu gửi không đổi; không chỉ dựa vào so sánh RowVersion trong service. PUT không đổi dữ liệu cũng có thể nhận RowVersion mới.
- Build mới ngoài sandbox thành công 0 warning/0 error. Chưa chạy HTTP/unit/integration cho PUT/concurrency; cần GET lấy RowVersion, PUT hợp lệ, gửi lại phiên bản cũ (kể cả body không đổi), validation/404/SKU trùng/danh mục thiếu. Quy tắc khóa cấu hình khi có OrderItem vẫn chưa triển khai vì chưa có OrderItem. Không commit.

## Cập nhật 10/11 — đã viết service UpdateAsync, PUT chưa hoàn tất

- Theo yêu cầu người dùng, trợ lý hoàn thiện ProductsService.UpdateAsync trả AdminProductResponse? (không phải DTO danh sách): lấy entity tracking/thiếu trảnull, validation DTO, chuẩn hóa, kiểm tra danh mục/SKU loại trừ ID hiện tại, gộp connectivity, đặt OriginalRowVersion, gán thông tin và tồn tuyệt đối, SaveChangAsync/map response.
- Sửa hai điểm cần thiết trong UpdateProductRequest: implement IValidatableObject để Validate được thực thi; RowVersion thành uint? với Required để phát hiện thiếu phiên bản. Giữ namespace hiện có ShopFlow.Api.Products. Có comment giải thích luồng học.
- Build mới ngoài sandbox thành công 0 warning/0 error. Chưa chạy HTTP/unit/integration cho UpdateAsync. Chưa thêm PUT controller hoặc chuyển DbUpdateConcurrencyException thành lỗi nghiệp vụ/409 trong lượt này; quy tắc khóa cấu hình sau OrderItem chưa triển khai (chưa có OrderItem). Cần kiểm chứng cả request không thay đổi dữ liệu với RowVersion cũ khi hoàn thiện concurrency. Không commit.

## Cập nhật 10/11 — đang học cập nhật sản phẩm

- Người dùng đã viết GetByIdForUpdateAsync có tracking/Include(Category)/FirstOrDefaultAsync và IsSkuExited kiểm tra SKU với Id khác sản phẩm đang sửa. Interface đã khai báo SetOriginalRowVersion.
- Theo yêu cầu chỉ viết hàm này, trợ lý thêm SetOriginalRowVersion vào ProductRepository: đặt Entry(product).Property(p => p.RowVersion).OriginalValue bằng phiên bản khách gửi. Chưa nối service/controller hoặc xử lý exception cập nhật trong lượt này; không coi PUT đã hoàn thành.
- Build mới: dotnet build ShopFlow.slnx --no-restore ngoài sandbox thành công 0 warning/0 error. Chưa kiểm thử concurrency/database cho cập nhật sản phẩm. Không commit.

## Cập nhật 10/11 — đã triển khai lọc switch/kết nối

- Người dùng yêu cầu trợ lý làm các phần lọc còn lại. Đã thêm SwitchType?/Connectivity? vào ProductQueryRequest, truyền xuyên service/interface/repository cho cả đếm và lấy trang. BuildActiveQuery so bằng SwitchType và dùng HasFlag cho Connectivity, kết hợp AND với các bộ lọc cũ. Không truyền thì bỏ qua.
- Service kiểm tra SwitchType là giá trị đã định nghĩa (bao gồm Unknown theo enum hiện có); Connectivity chỉ nhận Wired/Bluetooth/Wireless2_4G, từ chối None/tổ hợp/giá trị ngoài miền. Controller tiếp tục dùng query binding và trả400 cho đầu vào lỗi. Không đổi API Admin, không thêm package/migration, không commit.
- Build mới ngoài sandbox: dotnet build ShopFlow.slnx --no-restore thành công 0 warning/0 error. Chưa chạy HTTP/unit/integration hoặc rebuild Docker cho phần switch/connectivity. Cần kiểm tra switch riêng, kết nối riêng (sản phẩm nhiều kết nối vẫn khớp), kết hợp với giá/danh mục, TotalCount và enum sai/None/tổ hợp. Các bộ lọc catalog theo spec đã có code; chưa coi mọi case runtime đã pass.

## Cập nhật 10/11 — hướng dẫn lọc switch/kết nối trước khi triển khai

- Sau hướng dẫn thử lọc giá/sort, người dùng phản hồi “rồi ok rồi hướng dẫn tiếp”. Ghi nhận xác nhận chung của người dùng; không suy diễn mọi case/biên hoặc cả bốn kiểu sort đã được kiểm chứng riêng. Trợ lý chưa chạy HTTP mới.
- Bước học tiếp: thêm SwitchType nullable và Connectivity nullable vào DTO, validation enum và connectivity chỉ một giá trị Wired/Bluetooth/Wireless2_4G; truyền xuống bộ lọc dùng chung cho đếm/lấy trang. Switch so bằng; connectivity kiểm tra hỗ trợ cờ kết nối, không so bằng toàn bộ flags. Chưa triển khai phần này, không tự viết backend khi chỉ được yêu cầu hướng dẫn.

## Cập nhật 10/11 — triển khai lọc giá và sort

- Theo yêu cầu người dùng, trợ lý thêm MinPrice/MaxPrice nullable và Sort mặc định newest vào ProductQueryRequest; service kiểm tra giá không âm, min<=max khi có cả hai, sort chỉ nhận newest/priceAsc/priceDesc/nameAsc (trim trước kiểm tra). Không truyền một đầu khoảng giá thì bỏ qua đầu đó; hai biên dùng >= và <=.
- Repository dùng chung khoảng giá cho CountActiveAsync/GetActivePageAsync; sắp xếp trước Skip/Take. Newest: CreatedAt giảm/Id giảm; priceAsc: giá tăng/Id tăng; priceDesc: giá giảm/Id tăng; nameAsc: tên tăng/Id tăng. Không sửa luồng Admin, không thêm package/migration, không commit.
- Build mới ngoài sandbox: dotnet build ShopFlow.slnx --no-restore thành công 0 warning/0 error. Chưa rebuild Docker hoặc chạy HTTP/unit/integration cho lọc giá/sort; xác nhận Postman bên dưới chỉ thuộc bộ lọc Q/categoryId trước đó. Tiếp theo kiểm tra khoảng giá một/hai đầu, biên bằng nhau, 4 kiểu sort và lỗi giá âm/min>max/sort lạ. Switch/connectivity chưa triển khai.

## Cập nhật 10/11 — người dùng xác nhận các case lọc/tìm kiếm public

- Sau hướng dẫn Postman, người dùng xác nhận “test đúng hết” cho bộ case vừa giao: GET mặc định, Q riêng, categoryId riêng, kết hợp Q/categoryId/size=1, từ khóa không có kết quả, categoryId=0/page=0/size=101 trả400. Đây là xác nhận của người dùng, không phải trợ lý trực tiếp chạy HTTP. Chưa có bằng chứng riêng rằng dữ liệu có ít nhất hai sản phẩm khớp để kiểm chứng TotalCount lớn hơn Size; chưa xác nhận Q dài quá100, trim/chuỗi trắng hoặc offset cực lớn.
- Tiếp theo hướng dẫn lọc khoảng giá và sort xuyên suốt DTO/service/repository; sau đó switch/connectivity. Không sửa backend hoặc chạy lại build/test trong lượt ghi nhận này.

## Cập nhật 10/11 — triển khai lọc/tìm kiếm public

- Theo yêu cầu viết hộ, trợ lý hoàn thiện ProductQueryRequest người dùng đã tạo: Q nullable, CategoryId nullable, Page=1, Size=20; nối DTO vào ProductsController và GetPublicListAsync.
- Service trim Q, chuỗi trắng coi như không lọc; kiểm tra Q tối đa100 ký tự sau trim, CategoryId nếu có phải >0, page>=1, size1–100 và giới hạn offset. Repository dùng chung BuildActiveQuery cho đếm/lấy trang: active AND (Name.Contains hoặc Sku.Contains) AND danh mục nếu có; đếm trước phân trang, giữ sort CreatedAt/Id giảm dần. Contains hiện phân biệt hoa/thường theo PostgreSQL mặc định; chưa bổ sung tìm không phân biệt hoa/thường.
- Sửa phần code đang viết dở và đưa chữ ký/hành vi danh sách Admin về phân trang cũ; bộ lọc mới chỉ áp dụng public. Không thêm package/migration, không commit.
- Build mới: dotnet build ShopFlow.slnx --no-restore ngoài sandbox thành công 0 warning/0 error; sandbox trước đó thất bại không có chẩn đoán. Chưa chạy kiểm thử HTTP hoặc unit/integration cho bộ lọc, chưa rebuild API Docker. Tiếp theo kiểm tra không có bộ lọc, Q riêng, danh mục riêng, kết hợp, TotalCount và input sai. Khoảng giá/switch/connectivity/sort tùy chọn vẫn chưa làm.

## Kết phiên 10/11 — lịch sử trước khi viết lọc/tìm kiếm

- Đã kiểm tra code đã lưu: ProductsController có GET /api/products/{id} và GET /api/products?page=1&size=20, không yêu cầu đăng nhập. Chi tiết thiếu/inactive trả404; active hết hàng vẫn hiển thị. Repository danh sách lọc IsActive trước Skip/Take, Include(Category), AsNoTracking, sort CreatedAt/Id giảm dần; CountActiveAsync chỉ đếm active.
- ProductsResponse và ProductListResponse ở namespace Dtos.Products; Items là List<ProductsResponse>. GetPublicListAsync trả một ProductListResponse. Chi tiết và danh sách đã cùng dùng MapToPublicResponse; DTO public không có RowVersion, IsActive, CreatedAt.
- Người dùng báo đã kiểm tra Postman phần chi tiết rồi danh sách công khai. Đây là xác nhận chung của người dùng, không phải trợ lý trực tiếp chạy HTTP hoặc bằng chứng riêng cho mọi case/biên offset. Không chạy lại unit/integration test trong lượt kết phiên.
- Build kết phiên: dotnet build ShopFlow.slnx --no-restore thành công 0 warning/0 error. Không sửa backend hoặc commit trong lượt kết phiên.
- Còn nhỏ: thông báo validation đang ghi “Trang phai trong khoan tu 1-100”, nên đổi thành page >=1, size 1–100 khi tiếp tục; logic kiểm tra hiện đúng. Spec dòng DTO public còn ghi isActive/createdAt trong khi hướng dẫn và code hiện bỏ hai trường này; cần thống nhất trước khi chốt hợp đồng API, không tự sửa spec trong lượt này.
- QUAN TRỌNG: phần lọc/tìm kiếm vừa được hướng dẫn nhưng người dùng xác nhận CHƯA LÀM. Chưa có ProductQueryRequest, tìm theo q hoặc lọc categoryId; chưa có khoảng giá, switch/connectivity, sort tùy chọn. Không tick hoàn thành các phần này.
- Phiên sau bắt đầu ProductQueryRequest (Q, CategoryId, Page, Size), chuẩn hóa/validation, dùng cùng bộ lọc active + Name/Sku + CategoryId cho đếm và phân trang. Hướng dẫn mục tiêu trước, người dùng tự viết; chưa tự tạo DTO/khung khi chưa được nhờ. Không quay lại làm GET public cơ bản từ đầu.
- Đã tách quy tắc làm việc sang AGENTS.md theo yêu cầu; notes giữ tiến độ/lịch sử. Đã cập nhật bốn mô tả buổi12 trong learning-plan, giữ 30 buổi/120 ID; học/thực hành/kiểm chứng buổi12 còn dở do bao gồm filter/sort.
- Đã khởi động Docker Desktop và tracker riêng, không xóa volume. Đọc lại /api/progress xác nhận nội dung mới; day-12-review đã tick, day-12-learn/build/verify vẫn false. Tổng hiện tại 44/120 mục (37% làm tròn); giữ nguyên xác nhận/timestamp các mục khác.

## Kết phiên 09/11 — lịch sử: đã đọc sản phẩm Admin, tiếp tục catalog công khai

- Đã có GET /api/admin/products/{id} và GET /api/admin/products?page=1&size=20, yêu cầu Admin. Repository GetByIdAsync/GetPageAsync dùng AsNoTracking, Include(Category); danh sách sort CreatedAt rồi Id giảm dần, Skip/Take, không lọc IsActive. CountAsync đếm toàn bộ sản phẩm. Service GetListAsync kiểm tra page>=1, size 1–100, offset không vượt int.MaxValue; trả AdminProductListResponse Items/Page/Size/TotalCount. MapToResponse dùng chung cho GET chi tiết/danh sách; CreateAsync còn map riêng (không phải lỗi chặn).
- POST /api/admin/products đã đổi sang CreatedAtAction(nameof(GetById), id, response). User xác nhận GET chi tiết200, ID thiếu404, POST201 có Location và gọi GET lại được. User xác nhận bộ test danh sách page1/page2 size2, trang vượt dữ liệu trả200/items rỗng, page0 và size101 trả400. Đây là xác nhận người dùng qua Postman, không phải trợ lý trực tiếp chạy HTTP.
- Build kết phiên: dotnet build ShopFlow.slnx --no-restore thành công 0 warning/0 error. Không chạy lại test auth/category vì không sửa phần đó; kết quả 6 auth và11 category unit trước đây là lịch sử, không phải đợt test sản phẩm mới.
- Còn thiếu: các case input sai của POST sản phẩm chưa được user xác nhận riêng; Admin đọc inactive/hết hàng, auth Customer trên endpoint sản phẩm, biên offset cực lớn và concurrency PostgreSQL thật chưa kiểm chứng. Không tick kiểm chứng trọn bộ. Chưa có GET sản phẩm công khai, filter/search, PUT/DELETE sản phẩm.
- Phiên sau: hướng dẫn GET /api/products/{id} và danh sách /api/products công khai. DTO public không RowVersion; lọc IsActive, inactive chi tiết404, hết hàng active vẫn hiển thị. Tái sử dụng mapping hợp lý nhưng không trả nguyên AdminProductResponse cho khách. Sau đó bộ lọc/tìm kiếm/sort; không quay lại làm GET Admin từ đầu.
- Trong phiên trợ lý được nhờ hoàn thiện DTO, map response, GetListAsync và sửa các phần được yêu cầu.
- Web PersonalProgress đã có ô Tổng tiến độ dự án: đếm block done/tổng120, phần trăm làm tròn, tính cả buổi chưa hoàn thành; tự cập nhật cùng polling. Đã rebuild tracker riêng, giữ volume xác nhận. Lượt trước xác minh38/120 (32%); lượt kết phiên sẽ cập nhật thêm các mục đã học/thực hành/tổng kết, không dùng số cũ làm trạng thái hiện tại.
- Kế hoạch giữ30 buổi/120 ID. Buổi11 phản ánh đọc Admin đã làm; buổi12 chuyển thành catalog công khai + lọc/tìm kiếm. Tổng kết buổi10/11/13 ghi rõ việc còn thiếu. Không sửa backend hoặc commit trong lượt kết phiên này.

## Cập nhật tiến độ 09/11 — lịch sử trước GET sản phẩm Admin

- Category đã có PUT/DELETE. Quy tắc mới: có sản phẩm vẫn được đổi tên sau confirmRename=true; thiếu xác nhận trả 409 CATEGORY_RENAME_CONFIRMATION_REQUIRED. Tên không đổi không cần xác nhận; trùng danh mục khác 409. Transaction Serializable và ánh xạ xung đột 40001/40P01, kể cả commit. DELETE chặn danh mục có sản phẩm, bắt đúng FK_Products_Categories_CategoryId; thành công 204, thiếu ID 404.
- Bằng chứng: ảnh PUT id=2 Full-size 200; id=1 đổi 75% Demo chưa xác nhận 409, xác nhận true 200. User xác nhận các case GET đọc lại, PUT trùng/rỗng/ID thiếu đúng; user xác nhận bộ DELETE (có sản phẩm 409, danh mục tạm 204, xóa lại 404) chạy được. 11 unit cases CategoryUpdateTests pass; không chứng minh race/rollback PostgreSQL thật.
- Tạo sản phẩm đã có CreateProductRequest (IValidatableObject), AdminProductResponse, IProductRepository/ProductRepository, ProductsService, AdminProductsController POST /api/admin/products. DI và JSON enum chuỗi đã cấu hình, cấm enum dạng số. Chuẩn hóa SKU/chuỗi, kiểm tra danh mục/SKU, gộp flags, Add/Save/map DTO. Exception hiện tên ProductAlreadyExistException và CategoryNotFoundException; repository bắt đúng unique SKU và FK danh mục. Controller trả 201, SKU_EXISTS 409, CATEGORY_NOT_FOUND 400.
- Build gần nhất trong phiên: dotnet build ShopFlow.slnx --no-restore thành công 0 warning/0 error. User xác nhận POST sản phẩm hợp lệ 201 và gửi lại SKU 409. Các case validation sản phẩm đã hướng dẫn nhưng chưa được xác nhận riêng. Chưa kiểm thử cạnh tranh sản phẩm trên PostgreSQL thật.
- Việc tiếp theo: GET chi tiết sản phẩm Admin. Đã hướng dẫn repository AsNoTracking/Include Category/GetByIdAsync, service map AdminProductResponse dùng chung, controller GET 200/404; POST đổi StatusCode(201) sang CreatedAtAction khi GET đã có. Controller vừa đọc vẫn chỉ POST, chưa Location. Sau đó danh sách/public và phần CRUD sản phẩm còn lại.
- Trợ lý đã được nhờ viết DTO sản phẩm, map response và hoàn thiện cập nhật Category.
- Cập nhật web theo yêu cầu: ghi nội dung mới cho buổi10/11/13, giữ 30 buổi/120 ID; chỉ tick học/thực hành buổi10 và13, không tick các mục kiểm chứng chưa đầy đủ. Các ghi chép bên dưới là lịch sử, không phải trạng thái hiện tại. Không sửa backend/commit trong lượt cập nhật tiến độ này.

## Kết phiên 09/11 — lịch sử trước khi làm PUT/DELETE và sản phẩm

- Auth/đăng ký và đợt test đã hoàn thành phạm vi: 3 unit cases + 3 integration cases PostgreSQL test (thành công, rollback, race email có cổng đồng bộ), lần chạy trong phiên 6/6 pass. Không mở rộng test auth để trì hoãn CRUD. Build cuối phiên 09/11: dotnet build ShopFlow.slnx --no-restore, 0 warning/0 error. Không chạy lại test ở cuối phiên vì không sửa auth/test sau lần 6/6 pass.
- Đã đối chiếu appsettings.json: Jwt:ExpirationMinutes = 60. Docker/API/tracker đang tắt lúc bắt đầu kết phiên; không coi là lỗi code. Chưa có kiểm thử HTTP mới ở thời điểm kết phiên nếu dịch vụ chưa lên.
- Danh mục: CreateCategoryRequest (Required/MaxLength100), CategoryResponse (Id/Name); ICategoryRepository/CategoryRepository có GetAllAsync, GetByIdAsync, GetByNormalizedNameAsync, Add, SaveChangeAsync. Danh sách AsNoTracking, sort Name rồi Id; chỉ chuyển unique constraint IX_Categories_NormalizedName thành CategoryAlreadyExistsException.
- Class CategoryService nằm trong file Services/CategoriesService.cs (tên file số nhiều). CreateAsync trim an toàn null, kiểm tra 1–100 ký tự, ToLowerInvariant, kiểm tra trùng, tạo/lưu entity rồi map DTO. GetAllAsync/GetByIdAsync map DTO, chi tiết thiếu trả null. DI đã đăng ký đúng.
- GET /api/categories công khai; AdminCategoriesController yêu cầu Admin cho GET danh sách, GET /{id:int}, POST tạo (201 + CreatedAtAction Location, 400/409). CHƯA có PUT/DELETE.
- Bằng chứng runtime danh mục: ảnh POST TKL trả 201, id=2. GET /api/admin/categories/2 từng 401 do token; sau hướng dẫn đăng nhập/cập nhật token người dùng xác nhận “rồi được rồi”. Chưa có xác nhận riêng cho trùng TKL/tkl/khoảng trắng 409, tên rỗng/quá dài 400, GET công khai hoặc header Location. Không suy diễn “được rồi” thành tất cả case pass.
- Quy tắc mới theo yêu cầu người dùng: cho đổi tên danh mục có Product (kể cả inactive) sau khi Admin xác nhận. PUT nhận confirmRename mặc định false; nếu tên thay đổi và có Product mà chưa xác nhận, trả 409 CATEGORY_RENAME_CONFIRMATION_REQUIRED, không lưu. Giao diện hỏi xác nhận rồi gửi lại với confirmRename=true; tên hiển thị của tất cả sản phẩm thuộc danh mục sẽ thay đổi. Danh mục trống đổi tên không cần xác nhận. Giữ transaction Serializable quanh kiểm tra/cập nhật để xử lý thêm/chuyển Product đồng thời; xung đột trả 409. Xóa danh mục có Product vẫn bị chặn. Đây là thiết kế mới, chưa triển khai; user tự viết, không tick hoàn tất CRUD.
- User tự viết DTO/interface/repository/service; trợ lý sửa lỗi theo yêu cầu, hoàn thiện SaveChangeAsync, các lỗi validation, controller công khai và bổ sung controller Admin.
- Đã giải thích DI so với implement interface; async chỉ đặt ở phương thức có thân; Task<Category?> là kết quả có thể null; Category là layout có trước, Product chọn CategoryId. Giữ thiết kế tạo danh mục riêng, UI sau này có thể thêm nhanh danh mục cạnh dropdown.
- Không commit, không sửa backend trong lượt kết phiên. Các mục 08/11 và cũ hơn bên dưới là lịch sử.
- Đã khởi động Docker Desktop và PersonalProgress để cập nhật web. Đã sửa learning-plan.json, giữ 30 buổi/120 ID; web xác nhận buổi7–8 hoàn tất, buổi9 học/thực hành đã tick, kiểm chứng/tổng kết buổi9 và buổi10 vẫn chưa tick. Timestamp các mục đã hoàn thành trước đó được giữ nguyên. Việc seed Admin tự động và CancellationToken được ghi rõ chưa triển khai, không tính là đã làm. Không có kết quả HTTP danh mục mới từ trợ lý ở lượt kết phiên.

## Trạng thái 08/11 — ưu tiên hơn các mục lịch sử bên dưới

- Đã hoàn thiện JWT validation trong Program.cs, middleware authentication trước authorization; có GET /api/auth/me và GET /api/auth/admin-check yêu cầu Admin.
- Login hợp lệ 200 có ảnh; các trường hợp sai thông tin/validation người dùng xác nhận đúng. /me không token và token sai: trợ lý kiểm tra trực tiếp 401; token hợp lệ: ảnh 200 với Customer. admin-check: ảnh Customer bị 403. Người dùng xác nhận token hết hạn 401 và token Admin 200; không coi là trợ lý trực tiếp chạy hai trường hợp sau. Chưa đối chiếu lại cấu hình thời hạn đã khôi phục 60 phút.
- AuthService đã xử lý SuccessRehashNeeded: tạo hash từ mật khẩu vừa nhập, gán và lưu trước khi cấp JWT.
- AuthServiceTests: 3 unit scenarios Failed/Success/SuccessRehashNeeded, dùng fake, không chứng minh lưu DB thật.
- RegisterIntegrationTests: PostgreSQL test riêng localhost:5434/shopflow_test, service db-test trong Docker-compose.yaml. MigrateAsync tạo schema; mỗi test dùng email riêng, context mới xác minh và finally dọn dữ liệu.
- 3 integration scenarios: đăng ký lưu đúng User/Cart; trigger AFTER INSERT Cart gây lỗi có kiểm soát và xác nhận rollback User/Cart; hai đăng ký cùng email dùng DbContext riêng và cổng đồng bộ sau truy vấn email, cả hai thấy chưa tồn tại và thử lưu, đúng một thành công/một EmailAlreadyExistsException, DB đúng một User/một Cart. Đây là gọi service thật, không phải hai HTTP request.
- Bộ test đã chạy 6/6 pass. Test rollback gỡ trigger/function trong finally. Không sửa logic production để vượt test. Không commit.
- Đợt kiểm thử auth/đăng ký đã hoàn thành phạm vi đã chốt; tiếp theo CRUD danh mục, không mở rộng testing trước CRUD. Người dùng đã yêu cầu chuyển sang CRUD sau bài concurrency.
- User đã nhờ viết đầy đủ cấu hình JWT, /me, rehash và các test.

## Tiếp tục từ đây — trạng thái mới nhất

**Login/cấp JWT đã viết xong và build pass; CHƯA kiểm thử HTTP login. Phiên sau rebuild API Docker rồi thử POST http://localhost:8080/api/auth/login. Các mục ngày 04/11 bên dưới là lịch sử.**

- Register: người dùng xác nhận 201 khi hợp lệ; 409 khi trùng email/đổi hoa thường; 400 khi email sai/password ngắn/tên sau Trim quá ngắn. Người dùng kiểm tra DBeaver đúng Customer, hash, một User/một Cart và input sai không tạo user. Không coi đây là trợ lý trực tiếp truy vấn DB.
- Người dùng cung cấp output hai request đăng ký chạy nền: một 201, một 409; xác nhận đúng một User/một Cart. Chỉ là concurrency sơ bộ, chưa chứng minh race lúc lưu/rollback.
- ĐÃ CHỐT: xUnit + integration test rollback User/Cart và race email có kiểm soát làm SAU login/JWT validation/phân quyền và kiểm thử 401/403, TRƯỚC CRUD catalog. Không bắt quay lại rollback ngay; không tuyên bố atomic đã kiểm chứng.
- AuthService: ValidateCredentialsAsync chuẩn hóa email, tìm user, VerifyHashedPassword; không có user hoặc Failed trả null. SuccessRehashNeeded được chấp nhận nhưng chưa cập nhật hash; cần xử lý trước khi chốt auth. LoginAsync xác minh rồi mới cấp token, map đủ user Id/Email/FullName/Role.
- LoginResponse: AccessToken, TokenType=Bearer, ExpiresAt, User. LoginRequest: email validation, password Required, đã bỏ minimum length ở login.
- JwtTokenService: validate cấu hình, giải mã key Base64 ít nhất 32 byte, ký HS256, claims sub/role/jti; trả tuple AccessToken/ExpiresAt. Cùng biến expiresAt cho token/response; JWT exp chính xác giây, DateTime response có thể có phần lẻ giây.
- AuthController POST login: null trả 401 thông báo chung “Email hoặc mật khẩu không đúng”; hợp lệ Ok(response). Đã sửa tên ExpiresAt xuyên suốt và bỏ catch ArgumentException ở login.
- DI JwtTokenService đã đăng ký; package JwtBearer 10.0.13. appsettings Jwt: ShopFlow.Api/ShopFlow.Client/60 phút. JWT_KEY trong .env được Git bỏ qua, không tracked; Compose api truyền Jwt__Key. Đã kiểm tra biến hiện diện trong container khi cấu hình, không in key.
- Chưa AddAuthentication/AddJwtBearer/UseAuthentication, chưa /auth/me và phân quyền. Chưa rebuild container/test HTTP sau thay đổi login mới nhất.
- Trợ lý hoàn thiện xác minh password, token service, controller login và tên thời hạn theo yêu cầu; user tự viết DTO, inject, LoginAsync/map user.
- Build cuối phiên 06/11: dotnet build ShopFlow.slnx --no-restore ngoài sandbox, 0 warning/0 error. Không commit, không sửa backend khi kết phiên.
- Đã giải thích tầng Controller/Service/Repository/DbContext, DTO/entity; token khác chữ ký, key dùng kiểm tra chữ ký không phải giải mã nội dung.

## Trình tự phiên sau — ưu tiên hơn lịch sử bên dưới

1. Rebuild: docker compose -f Docker-compose.yaml up -d --build api. API chạy Docker cổng 8080. Lệnh build image/chạy container nền, không xóa dữ liệu DB hay tự migrate.
2. POST /api/auth/login: đúng 200 có token/expiresAt/user; sai password hoặc email chưa đăng ký 401 cùng thông báo. Chưa có bằng chứng runtime; không yêu cầu gửi token nguyên văn.
3. Cấu hình kiểm tra JWT dùng key giải mã Base64 giống bên ký, middleware authentication trước authorization, /auth/me và quyền; kiểm thử token thiếu/sai/hết hạn 401, Customer gọi Admin 403. Hoàn thiện SuccessRehashNeeded trước khi chốt auth.
4. Học xUnit, PostgreSQL test riêng, rollback đăng ký/race có kiểm soát, rồi mới CRUD catalog.
5. Kết phiên cập nhật notes/plan/web theo bằng chứng. Không commit.

## Lịch sử bàn giao 04/11 (đã được trạng thái trên thay thế)

## Tiếp tục từ đây

**Code đăng ký đã hoàn thiện và build pass. Phiên sau hướng dẫn kiểm thử POST /api/auth/register với database. Chưa có bằng chứng runtime cho đăng ký; chưa chuyển sang JWT.**

- AuthService đã trim/chuẩn hóa email, kiểm tra email rỗng hoặc tên ngoài 2–100 ký tự sau Trim, hash mật khẩu, tạo Customer và Cart, gắn navigation hai chiều, Add(user), await SaveChangeAsync một lần rồi trả Email/FullName.
- IUserRepository có Task<int> SaveChangeAsync(). UserRepository dùng async/await trên cùng DbContext; chỉ chuyển DbUpdateException có PostgresException, SqlState UniqueViolation (23505) và ConstraintName IX_Users_NormalizedEmail thành EmailAlreadyExistsException. Lỗi khác truyền ra.
- EmailAlreadyExistsException kế thừa Exception, constructor có thân và thông báo "Email da duoc su dung". Service dùng cùng lỗi khi phát hiện email trước khi lưu.
- AuthController đã có POST /api/auth/register: 201 kèm response, catch email trùng trả 409, ArgumentException trả 400. Không còn TODO/NotImplementedException.
- Đã giải thích DataAnnotations/[ApiController] validation khác bắt exception; thư mục Exceptions chỉ định nghĩa lỗi, không phải middleware. Chưa có middleware xử lý lỗi chung/JWT.
- Cart schema/migration đã xác nhận DB ngày 03/11; không làm lại. Chưa kiểm thử đăng ký, hash, hai request đồng thời hoặc rollback atomic.
- Kiểm tra cuối phiên 04/11: dotnet build ShopFlow.slnx --no-restore --no-incremental ngoài sandbox: 0 warning/0 error. Build trong sandbox thất bại không có chẩn đoán.
- Người dùng tự viết lưu/response/validation/exception; trợ lý viết bộ lọc lỗi repository và hoàn thiện controller theo yêu cầu.
- Bảng tiến độ buổi 6: học/thực hành xong; kiểm chứng/tổng kết nghiệp vụ chưa xong.
## Trình tự đề xuất cho phiên sau

1. Đọc notes/code đã lưu; không giao lại register/controller/schema đã xong.
2. Hướng dẫn chạy DB/API đúng môi trường và POST /api/auth/register: hợp lệ → 201; gửi lại email hoặc đổi hoa/thường → 409; email sai, password ngắn, tên sau Trim quá ngắn → 400.
3. Kiểm tra DB: đúng một Cart cho User, role Customer, password được hash và response không lộ hash. Kiểm thử đồng thời và rollback User/Cart bằng kiểm thử tích hợp có kiểm soát trước khi xác nhận atomic.
4. Sau kiểm chứng đăng ký mới login/JWT; kiểm tra dependency trước khi cài. Middleware exception chung thuộc bước xử lý lỗi sau này.
5. Khi kết phiên: kiểm tra code/build/bằng chứng, cập nhật notes + learning-plan + tick web. Không tự commit.
## Code và bằng chứng hiện có

### EF Core và database

- .NET 10, Controller–Service–Repository trong một API project; EF Core + PostgreSQL qua Npgsql.
- DbContext có DbSet Categories, Products, Users, Carts; DI UseNpgsql đọc DefaultConnection.
- `GET /health/db`: CanConnectAsync, 200/503. Người dùng từng báo test được; không coi kiểm tra kết nối là kiểm chứng schema.
- `Microsoft.EntityFrameworkCore.Design` 10.0.4 đã thêm để hỗ trợ EF tooling; dotnet ef đã kiểm tra là 10.0.14. Npgsql package vẫn `10.*` (lần đọc assets trước là EF Core 10.0.4); chưa khóa patch Npgsql.
- Category: Id, Name, NormalizedName, Products navigation. Hai tên required/max100; unique NormalizedName. IsRequired không chặn chuỗi rỗng.
- Product: đầy đủ SKU, giá decimal, Stock long, switch enum, connectivity Flags 1/2/4, bool? IsHotSwappable, UTC CreatedAt, uint RowVersion; CategoryId/navigation.
- Mapping Product: độ dài theo spec, SwitchType lưu chuỗi/check enum, Connectivity int/check1–7, numeric(18,2), Stock bigint, check Price>0/Stock>=0, unique SKU; index Name/CategoryId; RowVersion IsRowVersion ánh xạ xmin. Quan hệ Category–Product có DeleteBehavior.Restrict.
- User: Id, Email, NormalizedEmail, PasswordHash, FullName, UserRole, CreatedAt=UtcNow. Role Customer=0, Admin=1. Mapping Users: email/max254, hash/max512, fullname/max100, role string/max20/check Customer/Admin, timestamptz, unique NormalizedEmail.
- Migrations đã đọc và xác nhận cấu trúc đúng:
  - `20261031041454_InitialCreate`
  - `20261031102053_AddProducts`
  - `20261031141034_CreateUsersTable`
- Category/Product đã có bằng chứng đọc DB trực tiếp. Users và Carts: đã truy vấn schema/migration history trực tiếp ngày 03/11 và xác nhận đã áp dụng.

### Seed catalog

- `Data/DbSeeder.cs`: `Task<int> SeedAsync(ShopFlowDbContext db)` tìm Category theo NormalizedName="75%", thêm nếu thiếu; kiểm tra SKU="DEMO75-BLK-LIN", thêm Product nếu thiếu; gắn Category navigation. Seed không cập nhật tồn/giá sản phẩm hiện có.
- Dữ liệu mẫu: Brand Demo, màu Black, Linear, Wired, hot swap true, Price=1500000m, Stock=10; mô tả là dữ liệu giả.
- SaveChangesAsync nằm sau các nhánh và trả số entity đã lưu. Không return sớm chỉ vì Product đã tồn tại: Category mới (nếu có) vẫn cần lưu.
- Program.cs: khi args có --seed, tạo DI scope/lấy DbContext, await seed; kết quả 0 in không có thay đổi/không ghi đè, >0 in số bản ghi mới; return để không chạy web server. Chạy bình thường không seed hoặc tự migrate.
- Đã đọc DB bằng Docker/psql: Category Id=1, Name/NormalizedName=75%; SKU mẫu Id=1, CategoryId=1; mỗi truy vấn trả 1 dòng và join đúng. Người dùng không thấy Category lúc đầu, sau đã xác nhận thấy.
- Chưa có bằng chứng độc lập cho test đổi Stock=7 rồi seed lại giữ nguyên 7, test chạy hai lần sau thay đổi thông báo console, hay các negative test constraint. Không coi chúng đã pass.

### Kiểm tra gần nhất

- Các lần build toàn bộ sau sửa mapping User và tạo repository scaffold thành công 0 warning/0 error.
- Lần build khi kiểm tra repository trước đó vẫn có Add trống (build không phát hiện thiếu nghiệp vụ). Lần bàn giao 01/11 đã đọc thấy Add hoàn thiện; **không chạy build/test mới trong lượt chỉ cập nhật tài liệu này**.
- AuthService đã có code hash nhưng chưa hoàn chỉnh/chưa chạy endpoint; chưa controller/JWT hoặc kiểm thử nghiệp vụ. Test xUnit ban đầu là test rỗng.

## Các lệnh và lưu ý môi trường

Compose chính xác là `Docker-compose.yaml`. API host8080 → container8080; image shopflow-api:dev. PostgreSQL17, service db, host `127.0.0.1:5433` → container5432; volume postgres-data. `.env` chứa POSTGRES_USER/PASSWORD/DB, không in hoặc ghi secret vào notes/code.

| Nơi chạy | Host PostgreSQL | Port |
|---|---|---|
| EF CLI/API chạy trên Windows | localhost | 5433 |
| API trong Compose | db | 5432 |

Chỉ có một database trong Docker: Compose khởi động nó; EF chạy trên host kết nối để cập nhật schema. EF không tự đọc `.env` Compose. Người dùng đã nhầm CMD/PowerShell nhiều lần: nhìn prompt trước khi đưa lệnh.

PowerShell (prompt PS):
```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5433;Database=TEN_DB;Username=TEN_USER;Password=MAT_KHAU'
```
CMD:
```cmd
set "ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=TEN_DB;Username=TEN_USER;Password=MAT_KHAU"
```
Đặt trong cùng terminal chạy EF/seed; cửa sổ mới cần đặt lại. Thay placeholder bằng thông tin của người dùng, không yêu cầu gửi mật khẩu. Đã từng có comment chứa connection string thật trong DbContext; không chép lại secret, nhắc bỏ nếu còn xuất hiện.

Lệnh từ gốc ShopFlow:
```powershell
docker compose -f Docker-compose.yaml up -d db
docker compose -f Docker-compose.yaml ps
dotnet ef migrations add TEN_MIGRATION --project src/ShopFlow.Api
dotnet ef database update --project src/ShopFlow.Api
dotnet ef migrations list --project src/ShopFlow.Api
dotnet run --project src/ShopFlow.Api --no-launch-profile -- --seed
dotnet build ShopFlow.slnx --no-restore --no-incremental
```
Tên migration là tên tự đặt; EF so model với snapshot để quyết định thay đổi. Không tạo lại migration chỉ để đổi tên. Không dùng EnsureCreated/migration ngầm. Không dùng down -v khi cần giữ dữ liệu. Đổi POSTGRES_PASSWORD trong .env không tự đổi password DB đã khởi tạo.

Docker CLI tìm thấy khi chạy ngoài sandbox:
`C:\Users\Hi\AppData\Local\Programs\DockerDesktop\resources\bin\docker.exe`.
Sandbox có thể không thấy executable/config NuGet; xin escalation đúng quy định khi cần, không kết luận Docker chưa cài.

Hướng dẫn riêng: [migration-guide.md](docs/migration-guide.md) hiện viết theo CMD. [docker-checklist.md](docs/docker-checklist.md) là checklist dùng chung, không biến thành tài liệu riêng ShopFlow.

## Phạm vi dự án và việc còn lại

- Cửa hàng bàn phím cơ lắp sẵn; mỗi cấu hình là một SKU có kho riêng. Chỉ làm ShopFlow, GoldPulse riêng.
- React chưa bắt đầu; dự kiến web/, build vào wwwroot cùng container API, DB container riêng.
- MVP: mua hàng/quản trị/demo/chatbot catalog; reviews và báo cáo để sau. 7 bảng Users, Categories, Products, Carts, CartItems, Orders, OrderItems.
- JWT Admin/Customer; Customer hủy Pending, Admin hủy Pending/Paid; thanh toán mô phỏng, VND, không phí ship. Checkout/hủy phải atomic, không bán vượt tồn/hoàn kho hai lần; Cart.Revision là concurrency token.
- Chatbot chỉ đọc catalog/chính sách, không thao tác đơn/kho/dữ liệu riêng; chưa chọn provider/model/SDK.
- Còn: Cart/Order schema, register/login/JWT, CRUD catalog, cart/checkout/order, error handling/rate limit, frontend, chatbot, unit/integration tests, backup/restore/CI/README/deploy.
- 20 integration scenarios, 20 câu hỏi AI, 10 khách đồng thời là mục tiêu, chưa phải kết quả.
- Spec: [2026-10-30-shopflow-design.md](docs/superpowers/specs/2026-10-30-shopflow-design.md). Ưu tiên spec/notes mới hơn tài liệu Word công nghệ cũ.

## PersonalProgress — web theo dõi riêng

- Thư mục `C:\Users\Hi\Documents\PersonalProgress`; Node HTTP server không dependency, Docker Compose riêng (project personal-progress, service progress, Node24 Alpine), URL http://localhost:8091.
- Không gắn service tracker vào Compose hoặc API/database ShopFlow. Mount ShopFlow vào /project read-only; xác nhận lưu volume progress-data tại /data. Không dùng down -v.
- Nguồn nội dung web: `docs/learning-plan.json` trong ShopFlow, đọc lại mỗi 2 giây; plan.mjs là dự phòng. Notes/code cũng tự đọc lại; phát hiện code không chứng minh hoàn thành.
- Kế hoạch gồm 30 buổi/120 ID; 4 block mỗi buổi tổng240 phút dự kiến. Quy tắc cập nhật kế hoạch nằm trong AGENTS.md.
- Đã sửa 4 buổi đầu khớp Category → InitialCreate → Product/enum/navigation → mapping/FK/AddProducts/seed. Không yêu cầu bài tập Trim ngày1; triển khai khi làm CRUD Category. Nút Về việc tiếp theo ưu tiên việc chưa xác nhận thay vì ngày lịch.
- Quy ước mới 03/11: được người dùng ủy quyền tự tick khi kết phiên sau kiểm tra. Web đã có ngày1–4 hoàn thành; ngày5 cập nhật đúng Users/repository và xác nhận đủ; ngày6 chỉ phần học đã xác nhận. Bỏ tick day-6-build vì AuthService vẫn NotImplementedException; DTO/hash/Cart đã xong nhưng lưu và endpoint còn thiếu. Không thay timestamp các mục cũ không cần sửa.
- Ngày mặc định 30/10–28/11/2026, timezone Asia/Ho_Chi_Minh; PLAN_START_DATE thay lịch, xác nhận theo ID vẫn giữ.
- Đã test Node: giữ120 ID, giữ trạng thái/timestamp, không tự hoàn thành, đọc live plan không restart, lưu xác nhận và trả HTML. Đã rebuild/start Docker và gọi API/HTML localhost:8091 thành công ngày31/10; ảnh người dùng xác nhận giao diện mới. Không kiểm tra lại container trong lượt bàn giao này.
- Backup: `backup-aligned-plan-20261031-141141`; bản cũ `backup-before-daily-20261030-235608`, đều dưới PersonalProgress. .env/volume không bị ghi đè. Ghi ngoài workspace cần quyền môi trường.
- Chạy tại PersonalProgress: `docker compose up -d --build`; logs: `docker compose logs --tail 50 progress`. Chỉ đổi learning-plan.json/notes thì không cần rebuild; đổi server/index cần rebuild.

## Git và tài liệu

- Lúc bắt đầu bàn giao 01/11, git status sạch; HEAD `9005273 Add Docker, entities, migrations, repositories and health controller`. Trợ lý không tạo commit trong lượt này; sau bàn giao chỉ sửa notes.md.
- Không sửa backend, plan hoặc tracker trong lượt yêu cầu tóm tắt này.
- Nguồn học Docker người dùng cung cấp: `C:\Users\Hi\Downloads\[PUBLIC-YT] Docker Siêu Tốc@hoidanit.md`.
- Word tổng quan: `C:\Users\Hi\Documents\C#\Thiết kế 2 dự án .NET cho CV thực tập sinh Backend.docx`, bản backup cùng thư mục với hậu tố `.backup-2026-10-30.docx`; repo cũng có bản trong docs. Có thể còn công nghệ cũ; không sửa Word trong phiên này.

## Kết phiên 03/11/2026

- Đã cập nhật learning-plan.json: buổi5 Users/migration/repository; buổi6 gồm Cart bắt buộc và đăng ký atomic; JWT config chuyển buổi7. Giữ 30 buổi và 120 ID.
- Trạng thái web mục tiêu đã ghi qua /api/confirm: ngày1–5 hoàn thành; ngày6 học xong, thực hành/kiểm chứng/tổng kết nghiệp vụ còn dở. Không tuyên bố register hoàn thành.
- Code backend không bị trợ lý chỉnh trong lượt kết phiên. Không commit. Thay đổi backend hiện tại là bài người dùng, cần giữ.
- Các mục “lần bàn giao 01/11” bên dưới/trước là lịch sử, thông tin ngày03/11 ở đầu file là trạng thái ưu tiên.
## Kết phiên 04/11/2026

- Đã kiểm tra code đã lưu và build thành công. Không chỉnh backend hoặc commit trong lượt kết phiên.
- Đã cập nhật kế hoạch và tick thực hành buổi 6; kiểm chứng/tổng kết nghiệp vụ vẫn chưa xong.
- Các mục ngày 03/11 trở về trước là lịch sử; trạng thái đầu file ngày 04/11 được ưu tiên. Phiên sau bắt đầu kiểm thử API đăng ký.
