using ApplyMate.Core.Abstractions;

namespace ApplyMate.Core.Domain;

public sealed class JobApplication
{
    public const int JobNameMaxLength = 120;
    public const int CompanyNameMaxLength = 120;
    public const int JobSummaryMaxLength = 4000;
    public const int JobUrlMaxLength = 2000;

    private JobApplication()
    {
    }

    public Guid Id { get; private set; }

    public string JobName { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? JobSummary { get; private set; }

    public string? JobUrl { get; private set; }

    public DateOnly AppliedOn { get; private set; }

    public ApplicationStatus Status { get; private set; }

    public DateOnly LastStatusChangedOn { get; private set; }

    public DateTimeOffset? InterviewAt { get; private set; }

    public string? CvStoredPath { get; private set; }

    public string? CvOriginalFileName { get; private set; }

    public string? Notes { get; private set; }

    public static JobApplication Create(
        string jobName,
        string companyName,
        string? jobSummary,
        string? jobUrl,
        IDateProvider dateProvider,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(dateProvider);
        var today = dateProvider.Today;

        var app = new JobApplication
        {
            Id = id ?? Guid.NewGuid(),
            AppliedOn = today,
            Status = ApplicationStatus.Applied,
            LastStatusChangedOn = today
        };

        app.SetBasicInfo(jobName, companyName, jobSummary, jobUrl);
        return app;
    }

    public void InitializeForCreate(DateOnly today)
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        if (AppliedOn == default)
        {
            AppliedOn = today;
        }

        if (LastStatusChangedOn == default)
        {
            LastStatusChangedOn = today;
        }

        if (!Enum.IsDefined(Status))
        {
            Status = ApplicationStatus.Applied;
        }
    }

    public void SetBasicInfo(
        string jobName,
        string companyName,
        string? jobSummary,
        string? jobUrl)
    {
        JobName = NormalizeRequired(jobName, nameof(JobName), JobNameMaxLength);
        CompanyName = NormalizeRequired(companyName, nameof(CompanyName), CompanyNameMaxLength);
        JobSummary = NormalizeOptional(jobSummary, JobSummaryMaxLength);
        JobUrl = NormalizeOptional(jobUrl, JobUrlMaxLength);
    }

    public void SetAppliedOn(DateOnly appliedOn)
    {
        AppliedOn = appliedOn;
    }

    public void ChangeStatus(ApplicationStatus status, IDateProvider dateProvider)
    {
        ArgumentNullException.ThrowIfNull(dateProvider);
        ChangeStatus(status, dateProvider.Today);
    }

    public void ChangeStatus(ApplicationStatus status, DateOnly today)
    {
        Status = status;
        LastStatusChangedOn = today;
    }

    public bool TryApplyNoResponseRule(DateOnly today, int thresholdDays)
    {
        if (thresholdDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdDays), "Threshold days must be greater than zero.");
        }

        if (Status is not ApplicationStatus.Applied and not ApplicationStatus.InProgress)
        {
            return false;
        }

        var daysSinceChange = today.DayNumber - LastStatusChangedOn.DayNumber;
        if (daysSinceChange < thresholdDays)
        {
            return false;
        }

        ChangeStatus(ApplicationStatus.NoResponse, today);
        return true;
    }

    public void SetInterview(DateTimeOffset? interviewAt)
    {
        InterviewAt = interviewAt;
    }

    public void SetNotes(string? notes)
    {
        Notes = NormalizeOptional(notes, int.MaxValue);
    }

    public void AttachCv(string storedPath, string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            throw new ArgumentException("Stored path is required.", nameof(storedPath));
        }

        var normalizedFileName = NormalizeOptional(originalFileName, 255);
        if (string.IsNullOrWhiteSpace(normalizedFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        CvStoredPath = storedPath.Trim();
        CvOriginalFileName = normalizedFileName;
    }

    public void RemoveCv()
    {
        CvStoredPath = null;
        CvOriginalFileName = null;
    }

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters.",
                fieldName);
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                nameof(value));
        }

        return trimmed;
    }
}
