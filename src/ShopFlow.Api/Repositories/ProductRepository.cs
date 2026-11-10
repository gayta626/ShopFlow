using Npgsql;
using ShopFlow.Api.Exceptions;

using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ShopFlowDbContext _db;

    public ProductRepository(ShopFlowDbContext db)
    {
        _db = db;
    }

    public async Task<bool> GetBySkuAsync(string sku)
    {
        bool existing = await _db.Products.AnyAsync(p => p.Sku == sku);
        return existing;
    }

    public void Add(Product product)
    {
        _db.Products.Add(product);
    }

    public async Task<int> SaveChangAsync()
    {
        try
        {
            return await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // EF không cập nhật được bản ghi có Id và RowVersion đã yêu cầu.
            throw new ProductConcurrencyException(ex);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgres.ConstraintName == "IX_Products_Sku")
        {
            throw new ProductAlreadyExistException();
        }
        catch (DbUpdateException ex) when (
    ex.InnerException is PostgresException postgres &&
    postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
    postgres.ConstraintName == "FK_Products_Categories_CategoryId")
        {
            throw new CategoryNotFoundException();
        }
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product;
    }

    public async Task<int> CountAsync()
    {
        var totalProduct = await _db.Products.CountAsync();
        return totalProduct;
    }

    public async Task<List<Product>> GetPageAsync(int page, int size)
    {
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return products;
    }

    // Cùng một bộ lọc cho đếm và lấy trang để TotalCount khớp với Items.
    private IQueryable<Product> BuildActiveQuery(int? categoryId, string? query,
        decimal? minPrice, decimal? maxPrice, SwitchType? switchType, Connectivity? connectivity)
    {
        var products = _db.Products.AsNoTracking().Where(p => p.IsActive);

        if (categoryId.HasValue)
            products = products.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrEmpty(query))
            products = products.Where(p => p.Name.Contains(query) || p.Sku.Contains(query));

        if (minPrice.HasValue)
            products = products.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            products = products.Where(p => p.Price <= maxPrice.Value);

        if (switchType.HasValue)
            products = products.Where(p => p.SwitchType == switchType.Value);

        // Bàn phím hỗ trợ nhiều kết nối vẫn khớp nếu có kết nối khách chọn.
        if (connectivity.HasValue)
            products = products.Where(p => p.Connectivity.HasFlag(connectivity.Value));

        // Chưa thực thi SQL; hàm gọi sẽ đếm hoặc thêm phân trang.
        return products;
    }

    public async Task<int> CountActiveAsync(int? categoryId, string? query, decimal? minPrice, decimal? maxPrice,
        SwitchType? switchType, Connectivity? connectivity)
    {
        return await BuildActiveQuery(categoryId, query, minPrice, maxPrice, switchType, connectivity).CountAsync();
    }

    public async Task<List<Product>> GetActivePageAsync(int page, int size, int? categoryId, string? query,
        decimal? minPrice, decimal? maxPrice, string sort, SwitchType? switchType, Connectivity? connectivity)
    {
        var queryable = BuildActiveQuery(categoryId, query, minPrice, maxPrice, switchType, connectivity);

        // Sắp xếp toàn bộ kết quả đã lọc trước khi chọn trang; Id phá hòa.
        var ordered = sort switch
        {
            "newest" => queryable.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
            "priceAsc" => queryable.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "priceDesc" => queryable.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            "nameAsc" => queryable.OrderBy(p => p.Name).ThenBy(p => p.Id),
            _ => throw new ArgumentException("Kiểu sắp xếp không được hỗ trợ.")
        };

        var products = await ordered.Include(p => p.Category)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return products;
    }

    public async Task<Product?> GetByIdForUpdateAsync(int id)
    {
        var product = await _db.Products.Where(p => p.Id == id).Include(p => p.Category).FirstOrDefaultAsync();
        return product;
    }

    public async Task<bool> IsSkuExited(string sku, int productId)
    {
        var existing = await _db.Products.Where(p => p.Sku == sku && p.Id != productId).AnyAsync();
        return existing;
    }

    public void SetOriginalRowVersion(Product product, uint rowVersion)
    {

        _db.Entry(product)
            .Property(p => p.RowVersion)
            .OriginalValue = rowVersion;

        // PUT vẫn phải kiểm tra phiên bản tại database dù dữ liệu gửi không đổi.
        // Ghi lại Name hiện tại bảo đảm EF thực thi UPDATE có điều kiện RowVersion.
        _db.Entry(product).Property(p => p.Name).IsModified = true;
    }
}
