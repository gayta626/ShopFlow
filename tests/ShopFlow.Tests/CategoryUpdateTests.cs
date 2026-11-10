using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using ShopFlow.Api.Dtos.Categories;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Services;

namespace ShopFlow.Tests;

public class CategoryUpdateTests
{
    [Fact]
    public async Task ProductsWithoutConfirmation_DoNotChangeOrSave()
    {
        var repo = new FakeRepository { HasProducts = true };
        await Assert.ThrowsAsync<CategoryRenameConfirmationRequiredException>(() =>
            new CategoryService(repo).UpdateAsync(2, new() { Name = "TKL" }));
        Assert.Equal("TKLL", repo.Category!.Name);
        Assert.Equal(0, repo.Saves);
        Assert.False(repo.Transaction.Committed);
        Assert.True(repo.Transaction.Disposed);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task AllowedRename_TrimsNormalizesAndCommits(bool hasProducts, bool confirm)
    {
        var repo = new FakeRepository { HasProducts = hasProducts };
        var result = await new CategoryService(repo).UpdateAsync(2,
            new() { Name = " TKL ", ConfirmRename = confirm });
        Assert.Equal("TKL", result!.Name);
        Assert.Equal("tkl", repo.Category!.NormalizedName);
        Assert.Equal(1, repo.Saves);
        Assert.True(repo.Transaction.Committed);
    }

    [Fact]
    public async Task UnchangedName_DoesNotRequireConfirmation()
    {
        var repo = new FakeRepository { HasProducts = true };
        var result = await new CategoryService(repo).UpdateAsync(2, new() { Name = " TKLL " });
        Assert.Equal("TKLL", result!.Name);
        Assert.Equal(0, repo.Saves);
    }

    [Fact]
    public async Task MissingCategory_ReturnsNull()
    {
        var repo = new FakeRepository { Category = null };
        Assert.Null(await new CategoryService(repo).UpdateAsync(999, new() { Name = "TKL" }));
        Assert.Equal(0, repo.Saves);
    }

    [Fact]
    public async Task DuplicateOtherCategory_IsRejectedEvenWhenConfirmed()
    {
        var repo = new FakeRepository { Duplicate = new Category { Id = 3, Name = "TKL", NormalizedName = "tkl" } };
        await Assert.ThrowsAsync<CategoryAlreadyExistsException>(() =>
            new CategoryService(repo).UpdateAsync(2, new() { Name = "TKL", ConfirmRename = true }));
        Assert.Equal(0, repo.Saves);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task NameLength_RespectsInclusiveLimit(int length, bool valid)
    {
        var repo = new FakeRepository();
        var service = new CategoryService(repo);
        var request = new UpdateCategoryRequest { Name = new string('x', length) };
        if (valid) Assert.NotNull(await service.UpdateAsync(2, request));
        else await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(2, request));
    }

    [Theory]
    [InlineData("40001")]
    [InlineData("40P01")]
    public async Task CommitConflict_IsTranslatedAndTransactionDisposed(string sqlState)
    {
        var repo = new FakeRepository();
        repo.Transaction.CommitError = new PostgresException("conflict", "ERROR", "ERROR", sqlState);
        await Assert.ThrowsAsync<CategoryConcurrencyException>(() =>
            new CategoryService(repo).UpdateAsync(2, new() { Name = "TKL" }));
        Assert.False(repo.Transaction.Committed);
        Assert.True(repo.Transaction.Disposed);
    }

    // Fake kiểm tra nhánh nghiệp vụ; không chứng minh isolation/rollback PostgreSQL thật.
    private sealed class FakeRepository : ICategoryRepository
    {
        public Category? Category = new() { Id = 2, Name = "TKLL", NormalizedName = "tkll" };
        public Category? Duplicate;
        public bool HasProducts;
        public int Saves;
        public FakeTransaction Transaction = new();
        public Task<Category?> GetByIdAsync(int id) => Task.FromResult(Category?.Id == id ? Category : null);
        public Task<Category?> GetByNormalizedNameAsync(string name) => Task.FromResult(Duplicate ?? (Category?.NormalizedName == name ? Category : null));
        public Task<bool> HasProductAsync(int id) => Task.FromResult(HasProducts);
        public Task<int> SaveChangeAsync() => Task.FromResult(++Saves);
        public Task<IDbContextTransaction> BeginTransactionAsync() => Task.FromResult<IDbContextTransaction>(Transaction);
        public Task<List<Category>> GetAllAsync() => throw new NotSupportedException();
        public void Add(Category category) => throw new NotSupportedException();
        public void Remove(Category category) => throw new NotSupportedException();
    }

    private sealed class FakeTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public bool Committed;
        public bool Disposed;
        public Exception? CommitError;
        public void Commit() { if (CommitError != null) throw CommitError; Committed = true; }
        public Task CommitAsync(CancellationToken cancellationToken = default) { Commit(); return Task.CompletedTask; }
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
