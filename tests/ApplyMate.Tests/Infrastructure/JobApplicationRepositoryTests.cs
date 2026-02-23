using ApplyMate.Core.Domain;
using ApplyMate.Infrastructure.Persistence;
using ApplyMate.Infrastructure.Repositories;
using ApplyMate.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ApplyMate.Tests.Infrastructure;

public sealed class JobApplicationRepositoryTests
{
    [Fact]
    public async Task Crud_WorksAgainstSqlite()
    {
        await using var scope = await RepositoryTestScope.CreateAsync();
        var repository = new JobApplicationRepository(scope.ContextFactory);

        var createdOn = new DateOnly(2026, 2, 10);
        var app = JobApplication.Create(
            "Software Engineer",
            "Northwind",
            "Core platform role",
            "https://northwind.example/jobs/42",
            new FakeDateProvider(createdOn));

        await repository.AddAsync(app, CancellationToken.None);

        var loaded = await repository.GetByIdAsync(app.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Software Engineer", loaded!.JobName);
        Assert.Equal(ApplicationStatus.Applied, loaded.Status);

        loaded.ChangeStatus(ApplicationStatus.InProgress, new DateOnly(2026, 2, 12));
        loaded.SetInterview(new DateTimeOffset(2026, 2, 20, 10, 30, 0, TimeSpan.Zero));
        await repository.UpdateAsync(loaded, CancellationToken.None);

        var filtered = await repository.GetAllAsync(
            search: "Northwind",
            status: ApplicationStatus.InProgress,
            ct: CancellationToken.None);

        Assert.Single(filtered);
        Assert.Equal(app.Id, filtered[0].Id);

        await repository.DeleteAsync(app.Id, CancellationToken.None);
        var deleted = await repository.GetByIdAsync(app.Id, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ApplyNoResponseRule_UpdatesExpectedRows()
    {
        await using var scope = await RepositoryTestScope.CreateAsync();
        var repository = new JobApplicationRepository(scope.ContextFactory);

        var staleApplied = JobApplication.Create(
            "Backend Engineer",
            "A Corp",
            null,
            null,
            new FakeDateProvider(new DateOnly(2026, 1, 1)));

        var staleInProgress = JobApplication.Create(
            "Frontend Engineer",
            "B Corp",
            null,
            null,
            new FakeDateProvider(new DateOnly(2026, 1, 10)));
        staleInProgress.ChangeStatus(
            ApplicationStatus.InProgress,
            new DateOnly(2026, 1, 15));

        var unchangedRejected = JobApplication.Create(
            "QA Engineer",
            "C Corp",
            null,
            null,
            new FakeDateProvider(new DateOnly(2026, 1, 5)));
        unchangedRejected.ChangeStatus(
            ApplicationStatus.Rejected,
            new DateOnly(2026, 1, 6));

        await repository.AddAsync(staleApplied, CancellationToken.None);
        await repository.AddAsync(staleInProgress, CancellationToken.None);
        await repository.AddAsync(unchangedRejected, CancellationToken.None);

        var changed = await repository.ApplyNoResponseRuleAsync(
            today: new DateOnly(2026, 2, 14),
            thresholdDays: 30,
            ct: CancellationToken.None);

        Assert.Equal(2, changed);

        var staleAppliedUpdated = await repository.GetByIdAsync(staleApplied.Id, CancellationToken.None);
        var staleInProgressUpdated = await repository.GetByIdAsync(staleInProgress.Id, CancellationToken.None);
        var rejectedUpdated = await repository.GetByIdAsync(unchangedRejected.Id, CancellationToken.None);

        Assert.NotNull(staleAppliedUpdated);
        Assert.NotNull(staleInProgressUpdated);
        Assert.NotNull(rejectedUpdated);

        Assert.Equal(ApplicationStatus.NoResponse, staleAppliedUpdated!.Status);
        Assert.Equal(ApplicationStatus.NoResponse, staleInProgressUpdated!.Status);
        Assert.Equal(ApplicationStatus.Rejected, rejectedUpdated!.Status);
    }

    private sealed class RepositoryTestScope : IAsyncDisposable
    {
        private readonly string _databasePath;

        private RepositoryTestScope(string databasePath, IDbContextFactory<ApplyMateDbContext> contextFactory)
        {
            _databasePath = databasePath;
            ContextFactory = contextFactory;
        }

        public IDbContextFactory<ApplyMateDbContext> ContextFactory { get; }

        public static async Task<RepositoryTestScope> CreateAsync()
        {
            var tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "ApplyMate.Tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempDirectory);
            var dbPath = Path.Combine(tempDirectory, "applymate.db");

            var options = new DbContextOptionsBuilder<ApplyMateDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var contextFactory = new PooledDbContextFactory<ApplyMateDbContext>(options);

            await using (var db = await contextFactory.CreateDbContextAsync(CancellationToken.None))
            {
                await db.Database.EnsureCreatedAsync(CancellationToken.None);
            }

            return new RepositoryTestScope(dbPath, contextFactory);
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }

                var parent = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
                {
                    Directory.Delete(parent, recursive: true);
                }
            }
            catch
            {
            }

            return ValueTask.CompletedTask;
        }
    }
}
