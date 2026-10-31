using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail);
    void Add(User user);
}
