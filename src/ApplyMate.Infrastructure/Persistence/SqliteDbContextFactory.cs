using ApplyMate.Infrastructure.Persistence.Pathing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ApplyMate.Infrastructure.Persistence;

public sealed class SqliteDbContextFactory : IDbContextFactory<ApplyMateDbContext>
{
    private readonly string _databasePath;

    public SqliteDbContextFactory(IDatabasePathProvider databasePathProvider)
    {
        _databasePath = databasePathProvider.GetDatabasePath();
        var folderPath = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    public ApplyMateDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<ApplyMateDbContext>();
        builder.UseSqlite($"Data Source={_databasePath}");
        return new ApplyMateDbContext(builder.Options);
    }

    public Task<ApplyMateDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbContext());
    }
}
