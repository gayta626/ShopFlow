# ShopFlow React frontend

## Chạy tại máy

Từ gốc ShopFlow, dùng PowerShell:

```powershell
npm --prefix web ci
npm --prefix web run build
npm --prefix web run preview
```

Mở http://127.0.0.1:4173. API chạy tại http://127.0.0.1:8080; Vite proxy `/api` nên không cần mở CORS. Sau sửa source cần build lại để preview cập nhật. Chưa tích hợp React vào Docker/wwwroot ASP.NET.

Đường dẫn repo chứa `C#` khiến Vite dev không nạp đúng module trong môi trường đã kiểm tra. Dùng build/preview, không cần đổi tên hoặc di chuyển repo. Không đưa secret vào frontend.

## Chức năng hiện có

| Khu vực | Đường dẫn | Chức năng |
| --- | --- | --- |
| Cửa hàng | `/`, `/catalog`, `/products/{id}` | Danh sách, tìm kiếm/lọc/sắp xếp/phân trang; chi tiết và gallery minh họa |
| Xác thực | `/login`, `/register` | Đăng nhập, đăng ký; đăng ký xong chuyển đăng nhập |
| Chào mừng | `/welcome` | Poster sau đăng nhập và menu tài khoản theo vai trò |
| Hồ sơ | `/account` | Tên/email; Customer có tổng hợp và danh sách đơn; Admin có liên kết quản trị |
| Giỏ hàng | `/cart` | Thêm/sửa số lượng/xóa, xử lý lỗi tồn kho và trạng thái thay đổi |
| Đặt hàng | `/checkout` | Địa chỉ10–500 ký tự, tạo đơn demo và xem kết quả; chọn thẻ chỉ là Sắp hỗ trợ |
| Đơn Customer | `/orders/{id}` | Chi tiết; hủy Pending hoặc xác nhận nhận hàng khi Shipped |
| Đơn Admin | `/admin/orders`, `/admin/orders/{id}` | Lọc/phân trang; Pending→Paid/Cancelled, Paid→Shipped/Cancelled |
| Sản phẩm Admin | `/admin/products`, `/admin/products/new`, `/admin/products/{id}` | Phân trang, tạo/sửa với RowVersion, ngừng bán và bán lại |
| Danh mục Admin | `/admin/categories` | Danh sách, thêm, đổi tên có xác nhận khi có sản phẩm, xóa danh mục trống |

Đường dẫn không tồn tại hiển thị trang404. Token chỉ trong bộ nhớ: tải lại/đóng trang kết thúc phiên, không lưu localStorage/sessionStorage/cookie. Điều hướng nội bộ giữ phiên; `next` chỉ nhận đường dẫn được cho phép. Server quyết định quyền, frontend không thay thế kiểm tra quyền API.

## Hành vi cần lưu ý

- Checkout chỉ gửi ShippingAddress; giá/tổng/kho do server kiểm tra. Không có quote khóa giá hoặc idempotency key. Frontend khóa gửi lặp trong phiên nhưng không đảm bảo giữa tab/reload. Khi kết quả chưa rõ phải kiểm tra đơn trước khi đặt lại.
- Xác nhận Paid của Admin chỉ ghi nhận đã kiểm tra thanh toán, không thu tiền qua thẻ. Hủy đơn không tự hoàn tiền. Thanh toán thật chưa được tích hợp.
- Sửa tồn kho đặt số tồn mới tuyệt đối. PUT sản phẩm gửi RowVersion; xung đột cần tải lại. Cấu hình sản phẩm đã có trong đơn bị server khóa; API chưa có HasOrders để FE biết trước.
- Thống kê hồ sơ dùng6 truy vấn totalCount và1 truy vấn danh sách, có thể lệch tạm thời khi đơn thay đổi giữa các request; có nút làm mới.
- Cart response không có IsActive, nên thông báo unavailable gộp trường hợp ngừng bán/thiếu tồn.

## Kiểm chứng

Mở phiên Playwright CLI trước khi chạy các file `web/check-*.cjs`, ví dụ:

```powershell
playwright-cli -s=shopflow-react open http://127.0.0.1:4173 --browser msedge
playwright-cli -s=shopflow-react run-code --filename web/check-auth.cjs
```

Ngày22/11/2026 chạy mới: build; check-auth, check-cart, check-checkout, check-order, check-admin-orders, check-admin-products hoàn tất. Auth hiện dùng mock hoàn toàn, gồm401 sai thông tin, register201/409,503,me401,hết hạn,Back/reload,menu Admin,next và404. Cart đọc sản phẩm thật và kiểm tra thiếu token401; các thao tác ghi trong toàn bộ các script này đều được mock, không thay DB. Kiểm tra biểu mẫu responsive320–1440; không coi các ca giả lập là E2E tài khoản thật hoặc bằng chứng backend suite mới. Kết quả home/catalog/product cũ xem notes.md, không phải lần chạy mới.

## Phần chưa có / backend làm sau

- Đổi mật khẩu/quên mật khẩu/refresh token, xóa tài khoản, sổ địa chỉ và số điện thoại người nhận chưa có API tích hợp. Đọc/sửa tên và email đã nối GET/PUT /api/auth/me.
- Thanh toán thẻ thật: xem backlog trong `docs/superpowers/specs/2026-10-30-shopflow-design.md` (provider/merchant, phiên thanh toán, webhook, idempotency, hoàn tiền/đối soát và race kho). Không nhập số thẻ/CVV vào FE hiện tại.
- Ảnh/gallery theo SKU, upload ảnh, banner/nội dung và featured chưa có API. Mapping ảnh tạm trong `src/lib/productImages.js`; không khẳng định ảnh đúng SKU. Ảnh Keychron là tham khảo từ prototype, cần thay bằng tài sản được phép sử dụng trước khi phát hành công khai; font Be Vietnam Pro theo thiết kế hiện có.
- Admin sản phẩm API chỉ hỗ trợ page/size; chưa có tìm kiếm/lọc Admin. Không dựng bộ lọc giả trên một trang dữ liệu.
- Backend validation mật khẩu8–128 còn thiếu theo lần đọc trước; FE validation không thay kiểm tra server. Không sửa backend trong các bước frontend.

Tiến độ và bằng chứng chi tiết nằm trong `notes.md` ở gốc repo; mục mới nhất ưu tiên hơn ghi chú lịch sử.

23/11: Hồ sơ lấy dữ liệu GET /api/auth/me; dialog lưu họ tên/email bằng PUT và cập nhật phiên trong bộ nhớ từ response. Khóa gửi lặp, giữ input khi400/409,401 xóa phiên. Kết quả ghi chưa rõ yêu cầu GET lại trước khi sửa tiếp, không tự retry PUT. Build và check-auth mới pass với mock: PUT200/400/409/401/network, trim/body/Bearer, cập nhật tên khi điều hướng, Escape/reset và responsive320–1440. Chưa kiểm chứng lưu từ UI bằng tài khoản thật.

23/11: UI danh mục Admin đã nối API; check-admin-categories pass bằng mock cho thêm/trùng tên, đổi tên/xác nhận, xóa/in-use, conflict/network,401/Customer và responsive320–1440. Chưa kiểm chứng thao tác danh mục bằng tài khoản Admin thật.
