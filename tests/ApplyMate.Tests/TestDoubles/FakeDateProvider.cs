using ApplyMate.Core.Abstractions;

namespace ApplyMate.Tests.TestDoubles;

public sealed class FakeDateProvider : IDateProvider
{
    public FakeDateProvider(DateOnly today)
    {
        Today = today;
    }

    public DateOnly Today { get; set; }
}
