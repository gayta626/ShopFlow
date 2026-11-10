
using System.ComponentModel.DataAnnotations;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Dtos.Products;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Products;

namespace ShopFlow.Api.Services;

public class ProductsService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    public ProductsService(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<AdminProductResponse> CreateAsync(CreateProductRequest request)
    {
        string sku = request.Sku.Trim().ToUpperInvariant();
        string name = request.Name.Trim();
        string brand = request.Brand.Trim();
        string color = request.Color.Trim();
        string description = request.Description?.Trim() ?? string.Empty;

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new CategoryNotFoundException();
        }

        Connectivity connectivity = Connectivity.None;

        foreach (var connection in request.Connectivity)
        {
            connectivity |= connection;
        }

        bool skuExitst = await _productRepository.GetBySkuAsync(sku);
        if (skuExitst)
        {
            throw new ProductAlreadyExistException();
        }

        var product = new Product
        {
            Sku = sku,
            Name = name,
            Brand = brand,
            Color = color,
            Description = description,

            // Gắn sản phẩm vào danh mục đã tìm thấy.
            CategoryId = category.Id,
            Category = category,

            // Enum đơn lấy trực tiếp; danh sách kết nối đã được gộp.
            SwitchType = request.SwitchType,
            Connectivity = connectivity,

            // Giữ null nếu chưa biết có hỗ trợ thay switch nóng hay không.
            IsHotSwappable = request.IsHotSwappable,

            // Các giá trị đã qua validation DTO.
            Price = request.Price,
            Stock = request.Stock,
            IsActive = request.IsActive
        };

        _productRepository.Add(product);
        await _productRepository.SaveChangAsync();
        return new AdminProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Layout = category.Name,
            Sku = product.Sku,
            Name = product.Name,
            Brand = product.Brand,
            SwitchType = product.SwitchType,
            // Tách flags đã lưu thành danh sách các kết nối được hỗ trợ.
            Connectivity = new[]
            {
                Connectivity.Wired,
                Connectivity.Bluetooth,
                Connectivity.Wireless2_4G
            }.Where(connection => product.Connectivity.HasFlag(connection)).ToList(),
            Color = product.Color,
            IsHotSwappable = product.IsHotSwappable,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            RowVersion = product.RowVersion
        };


    }
    public async Task<AdminProductResponse?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return null;

        return MapToResponse(product);
    }

    // Dùng chung khi đọc chi tiết và danh sách sản phẩm.
    private static AdminProductResponse MapToResponse(Product product)
    {
        return new AdminProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Layout = product.Category.Name,
            Sku = product.Sku,
            Name = product.Name,
            Brand = product.Brand,
            SwitchType = product.SwitchType,
            // Tách flags đã lưu thành danh sách các kết nối được hỗ trợ.
            Connectivity = new[]
            {
                Connectivity.Wired,
                Connectivity.Bluetooth,
                Connectivity.Wireless2_4G
            }.Where(connection => product.Connectivity.HasFlag(connection)).ToList(),
            Color = product.Color,
            IsHotSwappable = product.IsHotSwappable,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            RowVersion = product.RowVersion
        };
    }
    public async Task<AdminProductListResponse> GetListAsync(int page, int size)
    {
        if (page < 1 || size < 1 || size > 100)
        {
            throw new ArgumentException("Page phải từ 1 trở lên; size phải từ 1 đến 100.");
        }
        long skipPage = (long)(page - 1) * size;
        if (skipPage > int.MaxValue)
        {
            throw new ArgumentException("Trang yêu cầu vượt giới hạn phân trang.");
        }

        // Đếm trước phân trang; chạy lần lượt vì repository dùng cùng DbContext.
        int totalCount = await _productRepository.CountAsync();
        var products = await _productRepository.GetPageAsync(page, size);

        return new AdminProductListResponse
        {
            Items = products.Select(MapToResponse).ToList(),
            Page = page,
            Size = size,
            TotalCount = totalCount
        };
    }

    public async Task<ProductsResponse?> GetPublicById(int id)
    {
        var products = await _productRepository.GetByIdAsync(id);

        if (products == null || products.IsActive == false) return null;

        return MapToPublicResponse(products);
    }


    private static ProductsResponse MapToPublicResponse(Product product)
    {
        return new ProductsResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Layout = product.Category.Name,
            Sku = product.Sku,
            Name = product.Name,
            Brand = product.Brand,
            SwitchType = product.SwitchType,
            // Tách flags đã lưu thành danh sách các kết nối được hỗ trợ.
            Connectivity = new[]
            {
                Connectivity.Wired,
                Connectivity.Bluetooth,
                Connectivity.Wireless2_4G
            }.Where(connection => product.Connectivity.HasFlag(connection)).ToList(),
            Color = product.Color,
            IsHotSwappable = product.IsHotSwappable,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
        };
    }
    public async Task<ProductListResponse> GetPublicListAsync(ProductQueryRequest request)
    {
        // Chuẩn hóa trước khi kiểm tra và truyền cùng giá trị cho cả hai truy vấn.
        string? keyWord = request.Q?.Trim();
        if (string.IsNullOrEmpty(keyWord)) keyWord = null;
        if (keyWord is { Length: > 100 })
            throw new ArgumentException("Từ khóa tối đa 100 ký tự.");
        if (request.CategoryId.HasValue && request.CategoryId.Value <= 0)
            throw new ArgumentException("Mã danh mục phải lớn hơn 0.");

        if (request.SwitchType.HasValue && !Enum.IsDefined(request.SwitchType.Value))
            throw new ArgumentException("Loại switch không hợp lệ.");
        if (request.Connectivity.HasValue && request.Connectivity.Value is not
            (Connectivity.Wired or Connectivity.Bluetooth or Connectivity.Wireless2_4G))
            throw new ArgumentException("Chọn một kết nối: Wired, Bluetooth hoặc Wireless2_4G.");

        if (request.MinPrice < 0 || request.MaxPrice < 0)
            throw new ArgumentException("Giá lọc không được âm.");
        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
            throw new ArgumentException("Giá tối thiểu không được lớn hơn giá tối đa.");

        string? sort = request.Sort?.Trim();
        if (sort is not ("newest" or "priceAsc" or "priceDesc" or "nameAsc"))
            throw new ArgumentException("Sort phải là newest, priceAsc, priceDesc hoặc nameAsc.");

        int page = request.Page;
        int size = request.Size;
        if (page < 1 || size < 1 || size > 100)
            throw new ArgumentException("Page phải từ 1 trở lên; size phải từ 1 đến 100.");
        long skipPage = (long)(page - 1) * size;

        if (skipPage > int.MaxValue) throw new ArgumentException("Trang yêu cầu vượt giới hạn phân trang.");

        // Đếm sau lọc, trước phân trang; chạy lần lượt trên cùng DbContext.
        int totalCount = await _productRepository.CountActiveAsync(
            request.CategoryId, keyWord, request.MinPrice, request.MaxPrice,
            request.SwitchType, request.Connectivity);
        var products = await _productRepository.GetActivePageAsync(
            page, size, request.CategoryId, keyWord, request.MinPrice, request.MaxPrice, sort,
            request.SwitchType, request.Connectivity);

        return new ProductListResponse
        {
            Items = products.Select(MapToPublicResponse).ToList(),
            Page = page,
            Size = size,
            TotalCount = totalCount
        };
    }

    public async Task<AdminProductResponse?> UpdateAsync(int id, UpdateProductRequest request)
    {
        // Lấy entity có tracking để EF lưu thay đổi trên chính sản phẩm này.
        var product = await _productRepository.GetByIdForUpdateAsync(id);
        if (product == null) return null;

        // Kiểm tra cả khi service được gọi trực tiếp, không đi qua controller.
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
            throw new ArgumentException(string.Join(" ", validationResults.Select(result => result.ErrorMessage)));

        string sku = request.Sku.Trim().ToUpperInvariant();
        string name = request.Name.Trim();
        string brand = request.Brand.Trim();
        string color = request.Color.Trim();
        string description = request.Description?.Trim() ?? string.Empty;

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null) throw new CategoryNotFoundException();

        // Giữ nguyên SKU của mình được phép; chỉ chặn SKU thuộc sản phẩm khác.
        if (await _productRepository.IsSkuExited(sku, id))
            throw new ProductAlreadyExistException();

        Connectivity connectivity = Connectivity.None;
        foreach (var connection in request.Connectivity)
            connectivity |= connection;

        // Phiên bản Admin đã đọc trở thành điều kiện kiểm tra khi EF lưu.
        // Required đã bảo đảm RowVersion có giá trị ở bước validation phía trên.
        _productRepository.SetOriginalRowVersion(product, request.RowVersion!.Value);

        product.Sku = sku;
        product.Name = name;
        product.Brand = brand;
        product.Color = color;
        product.Description = description;
        product.CategoryId = category.Id;
        product.Category = category;
        product.SwitchType = request.SwitchType;
        product.Connectivity = connectivity;
        product.IsHotSwappable = request.IsHotSwappable;
        product.Price = request.Price;
        // PUT đặt số tồn mới tuyệt đối, không cộng vào tồn hiện tại.
        product.Stock = request.Stock;
        product.IsActive = request.IsActive;

        await _productRepository.SaveChangAsync();
        return MapToResponse(product);

    }

}
