using ApplyMate.Core.Domain;

namespace ApplyMate.App.ViewModels;

public sealed record ApplicationStatusOption(string Label, ApplicationStatus? Value)
{
    public static readonly IReadOnlyList<ApplicationStatusOption> FilterOptions =
        new List<ApplicationStatusOption>
        {
        new("All", null),
        new("Applied", ApplicationStatus.Applied),
        new("In Progress", ApplicationStatus.InProgress),
        new("No Response", ApplicationStatus.NoResponse),
        new("Rejected", ApplicationStatus.Rejected),
        new("Accepted", ApplicationStatus.Accepted)
        };

    public static readonly IReadOnlyList<ApplicationStatusOption> SelectionOptions =
        new List<ApplicationStatusOption>
        {
        new("Applied", ApplicationStatus.Applied),
        new("In Progress", ApplicationStatus.InProgress),
        new("No Response", ApplicationStatus.NoResponse),
        new("Rejected", ApplicationStatus.Rejected),
        new("Accepted", ApplicationStatus.Accepted)
        };

    public static ApplicationStatusOption ForStatus(ApplicationStatus status)
    {
        var match = SelectionOptions.FirstOrDefault(x => x.Value == status);
        return match ?? SelectionOptions[0];
    }
}
