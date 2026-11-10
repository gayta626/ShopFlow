using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;

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

    public async Task<int> SaveChangeAsync()
    {
        try
        {
            return await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgresException.ConstraintName == "IX_Users_NormalizedEmail")
        {
            throw new EmailAlreadyExistsException();
        }
    }

}
