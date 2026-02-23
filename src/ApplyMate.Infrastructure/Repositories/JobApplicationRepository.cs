using ApplyMate.Core.Abstractions;
using ApplyMate.Core.Domain;
using ApplyMate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ApplyMate.Infrastructure.Repositories;

public sealed class JobApplicationRepository : IJobApplicationRepository
{
    private readonly IDbContextFactory<ApplyMateDbContext> _dbContextFactory;

    public JobApplicationRepository(
        IDbContextFactory<ApplyMateDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await db.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<JobApplication>> GetAllAsync(
        string? search,
        ApplicationStatus? status,
        CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<JobApplication> query = db.JobApplications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.JobName, pattern) ||
                EF.Functions.Like(x.CompanyName, pattern));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var items = await query.ToListAsync(ct);
        return items
            .OrderByDescending(x => x.AppliedOn)
            .ThenBy(x => x.InterviewAt ?? DateTimeOffset.MaxValue)
            .ToList();
    }

    public async Task<IReadOnlyList<JobApplication>> GetRecentAsync(int take, CancellationToken ct)
    {
        if (take <= 0)
        {
            return [];
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await db.JobApplications
            .AsNoTracking()
            .OrderByDescending(x => x.AppliedOn)
            .ThenByDescending(x => x.LastStatusChangedOn)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JobApplication>> GetUpcomingInterviewsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var items = await db.JobApplications
            .AsNoTracking()
            .Where(x => x.InterviewAt.HasValue)
            .ToListAsync(ct);

        return items
            .Where(x => x.InterviewAt.HasValue &&
                        x.InterviewAt.Value >= from &&
                        x.InterviewAt.Value <= to)
            .OrderBy(x => x.InterviewAt)
            .ToList();
    }

    public async Task<DashboardCounts> GetDashboardCountsAsync(CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var counts = await db.JobApplications
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new DashboardCounts(
                g.Count(),
                g.Count(x => x.Status == ApplicationStatus.Applied),
                g.Count(x => x.Status == ApplicationStatus.InProgress),
                g.Count(x => x.Status == ApplicationStatus.NoResponse),
                g.Count(x => x.Status == ApplicationStatus.Rejected),
                g.Count(x => x.Status == ApplicationStatus.Accepted)))
            .FirstOrDefaultAsync(ct);

        return counts ?? DashboardCounts.Empty;
    }

    public async Task AddAsync(JobApplication app, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        db.JobApplications.Add(app);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(JobApplication app, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        db.JobApplications.Update(app);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var existing = await db.JobApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
        {
            return;
        }

        db.JobApplications.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> ApplyNoResponseRuleAsync(DateOnly today, int thresholdDays, CancellationToken ct)
    {
        if (thresholdDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdDays), "Threshold days must be greater than zero.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var candidates = await db.JobApplications
            .Where(x => x.Status == ApplicationStatus.Applied ||
                        x.Status == ApplicationStatus.InProgress)
            .ToListAsync(ct);

        var changed = 0;
        foreach (var candidate in candidates)
        {
            if (candidate.TryApplyNoResponseRule(today, thresholdDays))
            {
                changed++;
            }
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return changed;
    }
}
