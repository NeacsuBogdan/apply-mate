using System.Diagnostics;
using System.IO;
using ApplyMate.App.Navigation;
using ApplyMate.Core.Abstractions;
using ApplyMate.Core.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace ApplyMate.App.ViewModels;

public sealed partial class ApplicationDetailsViewModel : ViewModelBase
{
    private readonly IJobApplicationRepository _repository;
    private readonly ICvStorageService _cvStorageService;
    private readonly IDateProvider _dateProvider;
    private readonly INavigationService _navigationService;

    private CancellationTokenSource? _loadCts;
    private JobApplication? _current;

    public ApplicationDetailsViewModel(
        IJobApplicationRepository repository,
        ICvStorageService cvStorageService,
        IDateProvider dateProvider,
        INavigationService navigationService)
    {
        _repository = repository;
        _cvStorageService = cvStorageService;
        _dateProvider = dateProvider;
        _navigationService = navigationService;

        StatusOptions = ApplicationStatusOption.SelectionOptions;
        SelectedStatus = StatusOptions[0];
        InterviewDate = DateTimeOffset.Now;
        InterviewTime = TimeSpan.FromHours(9);
    }

    public IReadOnlyList<ApplicationStatusOption> StatusOptions { get; }

    [ObservableProperty]
    private Guid _applicationId;

    [ObservableProperty]
    private string _jobName = string.Empty;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string? _jobSummary;

    [ObservableProperty]
    private string? _jobUrl;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private DateOnly _appliedOn;

    [ObservableProperty]
    private ApplicationStatusOption _selectedStatus;

    [ObservableProperty]
    private bool _hasInterview;

    [ObservableProperty]
    private DateTimeOffset _interviewDate;

    [ObservableProperty]
    private TimeSpan _interviewTime;

    [ObservableProperty]
    private string? _cvStoredPath;

    [ObservableProperty]
    private string? _cvOriginalFileName;

    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private string? _errorMessage;

    public string AppliedOnDisplay => AppliedOn.ToString("yyyy-MM-dd");

    public string CvDisplayName => string.IsNullOrWhiteSpace(CvOriginalFileName)
        ? "No CV attached"
        : CvOriginalFileName;

    public bool HasCv => !string.IsNullOrWhiteSpace(CvStoredPath);

    public Visibility InterviewEditorVisibility => HasInterview
        ? Visibility.Visible
        : Visibility.Collapsed;

    public async Task LoadAsync(Guid appId, CancellationToken ct)
    {
        _loadCts?.Cancel();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            ValidationMessage = null;

            var app = await _repository.GetByIdAsync(appId, _loadCts.Token);
            if (app is null)
            {
                ErrorMessage = "Application not found.";
                return;
            }

            _current = app;
            ApplicationId = app.Id;
            JobName = app.JobName;
            CompanyName = app.CompanyName;
            JobSummary = app.JobSummary;
            JobUrl = app.JobUrl;
            Notes = app.Notes;
            AppliedOn = app.AppliedOn;
            SelectedStatus = ApplicationStatusOption.ForStatus(app.Status);

            if (app.InterviewAt.HasValue)
            {
                var localInterview = app.InterviewAt.Value.ToLocalTime();
                HasInterview = true;
                InterviewDate = new DateTimeOffset(
                    localInterview.Year,
                    localInterview.Month,
                    localInterview.Day,
                    0,
                    0,
                    0,
                    localInterview.Offset);
                InterviewTime = localInterview.TimeOfDay;
            }
            else
            {
                HasInterview = false;
                InterviewDate = DateTimeOffset.Now;
                InterviewTime = TimeSpan.FromHours(9);
            }

            CvStoredPath = app.CvStoredPath;
            CvOriginalFileName = app.CvOriginalFileName;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_current is null)
        {
            ErrorMessage = "No application loaded.";
            return;
        }

        ValidationMessage = Validate();
        if (!string.IsNullOrWhiteSpace(ValidationMessage))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            _current.SetBasicInfo(JobName, CompanyName, JobSummary, JobUrl);
            _current.SetAppliedOn(AppliedOn);
            _current.SetNotes(Notes);

            var newStatus = SelectedStatus.Value ?? ApplicationStatus.Applied;
            if (_current.Status != newStatus)
            {
                _current.ChangeStatus(newStatus, _dateProvider);
            }

            if (HasInterview)
            {
                var localInterview = InterviewDate.Date + InterviewTime;
                var offset = TimeZoneInfo.Local.GetUtcOffset(localInterview);
                _current.SetInterview(new DateTimeOffset(localInterview, offset));
            }
            else
            {
                _current.SetInterview(null);
            }

            await _repository.UpdateAsync(_current, CancellationToken.None);
            _navigationService.NavigateTo(PageKeys.Applications, clearBackStack: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_current is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (!string.IsNullOrWhiteSpace(_current.CvStoredPath))
            {
                await _cvStorageService.RemoveCvAsync(_current.CvStoredPath, CancellationToken.None);
            }

            await _repository.DeleteAsync(_current.Id, CancellationToken.None);
            _navigationService.NavigateTo(PageKeys.Applications, clearBackStack: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveCvAsync()
    {
        if (_current is null || string.IsNullOrWhiteSpace(_current.CvStoredPath))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await _cvStorageService.RemoveCvAsync(_current.CvStoredPath, CancellationToken.None);
            _current.RemoveCv();
            await _repository.UpdateAsync(_current, CancellationToken.None);

            CvStoredPath = null;
            CvOriginalFileName = null;
            OnPropertyChanged(nameof(CvDisplayName));
            OnPropertyChanged(nameof(HasCv));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenCv()
    {
        if (string.IsNullOrWhiteSpace(CvStoredPath) || !File.Exists(CvStoredPath))
        {
            ErrorMessage = "CV file is not available.";
            return;
        }

        try
        {
            Process.Start(
                new ProcessStartInfo(CvStoredPath)
                {
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task AttachCvFromPathAsync(string sourceFilePath, CancellationToken ct)
    {
        if (_current is null)
        {
            ErrorMessage = "No application loaded.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (!string.IsNullOrWhiteSpace(_current.CvStoredPath))
            {
                await _cvStorageService.RemoveCvAsync(_current.CvStoredPath, ct);
            }

            var (storedPath, originalFileName) = await _cvStorageService.CopyCvIntoLocalAsync(
                _current.Id,
                sourceFilePath,
                ct);

            _current.AttachCv(storedPath, originalFileName);
            await _repository.UpdateAsync(_current, ct);

            CvStoredPath = storedPath;
            CvOriginalFileName = originalFileName;
            OnPropertyChanged(nameof(CvDisplayName));
            OnPropertyChanged(nameof(HasCv));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnAppliedOnChanged(DateOnly value)
    {
        OnPropertyChanged(nameof(AppliedOnDisplay));
    }

    partial void OnCvStoredPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasCv));
    }

    partial void OnCvOriginalFileNameChanged(string? value)
    {
        OnPropertyChanged(nameof(CvDisplayName));
    }

    partial void OnHasInterviewChanged(bool value)
    {
        OnPropertyChanged(nameof(InterviewEditorVisibility));
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(JobName))
        {
            return "Job name is required.";
        }

        if (JobName.Trim().Length > JobApplication.JobNameMaxLength)
        {
            return $"Job name cannot exceed {JobApplication.JobNameMaxLength} characters.";
        }

        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            return "Company name is required.";
        }

        if (CompanyName.Trim().Length > JobApplication.CompanyNameMaxLength)
        {
            return $"Company name cannot exceed {JobApplication.CompanyNameMaxLength} characters.";
        }

        if (!string.IsNullOrWhiteSpace(JobSummary) &&
            JobSummary.Trim().Length > JobApplication.JobSummaryMaxLength)
        {
            return $"Job summary cannot exceed {JobApplication.JobSummaryMaxLength} characters.";
        }

        if (!string.IsNullOrWhiteSpace(JobUrl) &&
            JobUrl.Trim().Length > JobApplication.JobUrlMaxLength)
        {
            return $"Job URL cannot exceed {JobApplication.JobUrlMaxLength} characters.";
        }

        return null;
    }
}
