namespace ApplyMate.Core.Abstractions;

public interface IDateProvider
{
    DateOnly Today { get; }
}
