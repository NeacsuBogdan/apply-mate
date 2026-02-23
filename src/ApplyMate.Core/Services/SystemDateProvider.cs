using ApplyMate.Core.Abstractions;

namespace ApplyMate.Core.Services;

public sealed class SystemDateProvider : IDateProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
