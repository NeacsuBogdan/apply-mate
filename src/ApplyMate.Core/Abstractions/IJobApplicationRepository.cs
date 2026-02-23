using ApplyMate.Core.Domain;

namespace ApplyMate.Core.Abstractions;

public interface IJobApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<JobApplication>> GetAllAsync(
        string? search,
        ApplicationStatus? status,
        CancellationToken ct);

    Task<IReadOnlyList<JobApplication>> GetRecentAsync(int take, CancellationToken ct);

    Task<IReadOnlyList<JobApplication>> GetUpcomingInterviewsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<DashboardCounts> GetDashboardCountsAsync(CancellationToken ct);

    Task AddAsync(JobApplication app, CancellationToken ct);

    Task UpdateAsync(JobApplication app, CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<int> ApplyNoResponseRuleAsync(DateOnly today, int thresholdDays, CancellationToken ct);
}
