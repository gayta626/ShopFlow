using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ShopFlowDbContext _db;

    public UserRepository(ShopFlowDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail)
    {
        return _db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

    }

    public void Add(User user)
    {
        _db.Users.Add(user);
    }
}
