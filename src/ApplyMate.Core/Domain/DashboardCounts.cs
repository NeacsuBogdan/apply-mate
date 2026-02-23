namespace ApplyMate.Core.Domain;

public sealed record DashboardCounts(
    int Total,
    int Applied,
    int InProgress,
    int NoResponse,
    int Rejected,
    int Accepted)
{
    public static DashboardCounts Empty { get; } = new(0, 0, 0, 0, 0, 0);
}
