using ApplyMate.Core.Domain;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ApplyMate.App.ViewModels;

public sealed class ApplicationListItemViewModel
{
    public ApplicationListItemViewModel(JobApplication source)
    {
        Source = source;

        CompanyName = source.CompanyName;
        JobName = source.JobName;
        AppliedOnText = source.AppliedOn.ToString("yyyy-MM-dd");
        InterviewAtText = source.InterviewAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "No interview";
        StatusText = StatusToText(source.Status);
        StatusBadgeBrush = StatusToBrush(source.Status);
    }

    public JobApplication Source { get; }

    public Guid Id => Source.Id;

    public string CompanyName { get; }

    public string JobName { get; }

    public string AppliedOnText { get; }

    public string InterviewAtText { get; }

    public string StatusText { get; }

    public Brush StatusBadgeBrush { get; }

    private static string StatusToText(ApplicationStatus status) =>
        status switch
        {
            ApplicationStatus.InProgress => "In Progress",
            ApplicationStatus.NoResponse => "No Response",
            _ => status.ToString()
        };

    private static Brush StatusToBrush(ApplicationStatus status)
    {
        var color = status switch
        {
            ApplicationStatus.Applied => Color.FromArgb(58, 0, 120, 212),
            ApplicationStatus.InProgress => Color.FromArgb(58, 180, 102, 0),
            ApplicationStatus.NoResponse => Color.FromArgb(58, 96, 96, 96),
            ApplicationStatus.Rejected => Color.FromArgb(58, 193, 36, 36),
            ApplicationStatus.Accepted => Color.FromArgb(58, 0, 128, 64),
            _ => Color.FromArgb(45, 96, 96, 96)
        };

        return new SolidColorBrush(color);
    }
}
