

using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql; // Đọc mã lỗi và tên constraint của PostgreSQL.
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;

namespace ShopFlow.Api.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ShopFlowDbContext _db;

    public CategoryRepository(ShopFlowDbContext db) // Repository chỉ cần context, không inject chính interface của nó.
    {
        _db = db;

    }

    public async Task<List<Category>> GetAllAsync()
    {
        var result = await _db.Categories
            .AsNoTracking() // Danh sách chỉ để đọc, không cần EF theo dõi thay đổi.
            .OrderBy(c => c.Name).ThenBy(c => c.Id) // Thứ tự ổn định khi trả danh sách.
            .ToListAsync();
        return result;
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        var result = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        return result;
    }

    public async Task<Category?> GetByNormalizedNameAsync(string normalizedName)
    {
        var result = await _db.Categories.FirstOrDefaultAsync(c => c.NormalizedName == normalizedName);
        return result;
    }


    public void Add(Category category)
    {
        _db.Categories.Add(category);
    }
    public async Task<int> SaveChangeAsync()
    {
        try
        {
            return await _db.SaveChangesAsync(); // Lưu thay đổi và trả số entity đã lưu; await để catch bắt được lỗi async.
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException postgresException && // Lỗi gốc đến từ PostgreSQL.
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation && // Vi phạm ràng buộc unique (23505).
            postgresException.ConstraintName == "IX_Categories_NormalizedName") // Chỉ bắt lỗi trùng tên danh mục.
        {
            throw new CategoryAlreadyExistsException(); // Chuyển lỗi database thành lỗi nghiệp vụ; các lỗi khác truyền ra ngoài.
        }
        catch (DbUpdateException ex) when (
    ex.InnerException is PostgresException postgres &&
    postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation &&
    postgres.ConstraintName == "FK_Products_Categories_CategoryId")
        {
            throw new CategoryInUseException();
        }
    }

    public async Task<bool> HasProductAsync(int cateId)
    {
        var isExisted = await _db.Products.AnyAsync(p => p.CategoryId == cateId);

        return isExisted;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    }

    public void Remove(Category category)
    {
        _db.Categories.Remove(category);
    }
}
