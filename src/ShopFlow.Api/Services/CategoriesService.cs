

using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShopFlow.Api.Dtos.Categories;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Repositories;

namespace ShopFlow.Api.Services;

public class CategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        string name = request.Name?.Trim() ?? string.Empty;
        if (name.Length < 1 || name.Length > 100)
        {
            throw new ArgumentException("Tên danh mục phải có từ 1 đến 100 ký tự.");
        }
        string normailizedName = name.ToLowerInvariant();
        var checkExisted = await _categoryRepository.GetByNormalizedNameAsync(normailizedName);
        if (checkExisted != null)
        {
            throw new CategoryAlreadyExistsException();
        }

        Category category = new Category()
        {
            Name = name,
            NormalizedName = normailizedName
        };
        _categoryRepository.Add(category);
        await _categoryRepository.SaveChangeAsync();
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name
        };
    }

    public async Task<List<CategoryResponse>> GetAllAsync()
    {
        var listCategory = await _categoryRepository.GetAllAsync();
        var responses = listCategory.Select(c => new CategoryResponse { Id = c.Id, Name = c.Name }).ToList();
        return responses;
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return null;

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
        };
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        string name = request.Name?.Trim() ?? string.Empty;
        string normalizedName = name.ToLowerInvariant();

        if (name.Length < 1 || name.Length > 100)
        {
            throw new ArgumentException("Tên danh mục phải có từ 1 đến 100 ký tự.");
        }

        try
        {

            await using var transaction = await _categoryRepository.BeginTransactionAsync();
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return null;

            if (category.Name == name)
                return new CategoryResponse { Id = category.Id, Name = category.Name };

            var existing = await _categoryRepository.GetByNormalizedNameAsync(normalizedName);
            if (existing != null && existing.Id != id)
                throw new CategoryAlreadyExistsException();

            bool hasProducts = await _categoryRepository.HasProductAsync(id);
            if (hasProducts && !request.ConfirmRename)
                throw new CategoryRenameConfirmationRequiredException();


            category.Name = name;
            category.NormalizedName = normalizedName;
            await _categoryRepository.SaveChangeAsync();
            await transaction.CommitAsync();

            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name
            };
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            // Bắt cả lỗi truy vấn, SaveChanges và commit; không tự retry.
            throw new CategoryConcurrencyException(ex);
        }
    }

    private static bool IsConcurrencyConflict(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException)
                return true;
            if (current is PostgresException postgres &&
                postgres.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
                return true;
        }
        return false;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return false;
        bool hasProducts = await _categoryRepository.HasProductAsync(id);
        if (hasProducts)
        {
            throw new CategoryInUseException();
        }

        _categoryRepository.Remove(category);
        await _categoryRepository.SaveChangeAsync();

        return true;
    }
}
