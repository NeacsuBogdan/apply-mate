using ApplyMate.Core.Domain;
using ApplyMate.Tests.TestDoubles;

namespace ApplyMate.Tests.Core;

public sealed class JobApplicationBusinessRulesTests
{
    [Fact]
    public void ChangeStatus_UpdatesLastStatusChangedOn()
    {
        var createdOn = new DateOnly(2026, 2, 1);
        var dateProvider = new FakeDateProvider(createdOn);
        var application = JobApplication.Create(
            "Backend Engineer",
            "Contoso",
            null,
            null,
            dateProvider);

        var changedOn = new DateOnly(2026, 2, 5);
        application.ChangeStatus(ApplicationStatus.InProgress, changedOn);

        Assert.Equal(ApplicationStatus.InProgress, application.Status);
        Assert.Equal(changedOn, application.LastStatusChangedOn);
    }

    [Fact]
    public void ApplyNoResponseRule_RespectsBoundaryDays()
    {
        var createdOn = new DateOnly(2026, 1, 1);
        var dateProvider = new FakeDateProvider(createdOn);
        var application = JobApplication.Create(
            "Desktop Developer",
            "Fabrikam",
            null,
            null,
            dateProvider);

        var day29 = new DateOnly(2026, 1, 30);
        var changedAt29Days = application.TryApplyNoResponseRule(day29, thresholdDays: 30);

        Assert.False(changedAt29Days);
        Assert.Equal(ApplicationStatus.Applied, application.Status);
        Assert.Equal(createdOn, application.LastStatusChangedOn);

        var day30 = new DateOnly(2026, 1, 31);
        var changedAt30Days = application.TryApplyNoResponseRule(day30, thresholdDays: 30);

        Assert.True(changedAt30Days);
        Assert.Equal(ApplicationStatus.NoResponse, application.Status);
        Assert.Equal(day30, application.LastStatusChangedOn);
    }
}
