using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ApplyMate.Infrastructure.Persistence;

public interface IApplyMateDatabaseInitializer
{
    Task EnsureCreatedAsync(CancellationToken ct);
}

public sealed class ApplyMateDatabaseInitializer : IApplyMateDatabaseInitializer
{
    private readonly IDbContextFactory<ApplyMateDbContext> _dbContextFactory;

    public ApplyMateDatabaseInitializer(IDbContextFactory<ApplyMateDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);
    }
}
