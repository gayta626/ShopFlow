
namespace ShopFlow.Api.Services;

public interface IProductImageStorage
{
    public Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken);

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken);
}