using ApplyMate.App.Navigation;
using ApplyMate.Core.Abstractions;
using ApplyMate.Core.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace ApplyMate.App.ViewModels;

public sealed partial class AddApplicationViewModel : ViewModelBase
{
    private readonly IJobApplicationRepository _repository;
    private readonly ICvStorageService _cvStorageService;
    private readonly IDateProvider _dateProvider;
    private readonly INavigationService _navigationService;

    public AddApplicationViewModel(
        IJobApplicationRepository repository,
        ICvStorageService cvStorageService,
        IDateProvider dateProvider,
        INavigationService navigationService)
    {
        _repository = repository;
        _cvStorageService = cvStorageService;
        _dateProvider = dateProvider;
        _navigationService = navigationService;

        AppliedOn = _dateProvider.Today;
    }

    [ObservableProperty]
    private string _jobName = string.Empty;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string? _jobSummary;

    [ObservableProperty]
    private string? _jobUrl;

    [ObservableProperty]
    private DateOnly _appliedOn;

    [ObservableProperty]
    private string? _pendingCvSourcePath;

    [ObservableProperty]
    private string? _pendingCvFileName;

    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private string? _errorMessage;

    public string AppliedOnDisplay => AppliedOn.ToString("yyyy-MM-dd");
    public bool HasPendingCv => !string.IsNullOrWhiteSpace(PendingCvSourcePath);
    public string PendingCvDisplayName => string.IsNullOrWhiteSpace(PendingCvFileName)
        ? "No CV attached"
        : PendingCvFileName;

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = Validate();
        if (!string.IsNullOrWhiteSpace(ValidationMessage))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var appId = Guid.NewGuid();
            string? copiedCvPath = null;
            string? copiedCvOriginalName = null;

            var created = JobApplication.Create(
                JobName,
                CompanyName,
                JobSummary,
                JobUrl,
                _dateProvider,
                appId);

            created.SetAppliedOn(AppliedOn);

            if (!string.IsNullOrWhiteSpace(PendingCvSourcePath))
            {
                (copiedCvPath, copiedCvOriginalName) = await _cvStorageService.CopyCvIntoLocalAsync(
                    appId,
                    PendingCvSourcePath,
                    CancellationToken.None);

                created.AttachCv(copiedCvPath, copiedCvOriginalName);
            }

            await _repository.AddAsync(created, CancellationToken.None);

            _navigationService.NavigateTo(PageKeys.Dashboard, clearBackStack: true);
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
    private void Cancel()
    {
        _navigationService.NavigateTo(PageKeys.Dashboard);
    }

    [RelayCommand]
    private void RemoveCv()
    {
        PendingCvSourcePath = null;
        PendingCvFileName = null;
        OnPropertyChanged(nameof(HasPendingCv));
        OnPropertyChanged(nameof(PendingCvDisplayName));
    }

    public void SetCvFromPath(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        PendingCvSourcePath = sourcePath;
        PendingCvFileName = Path.GetFileName(sourcePath);
        OnPropertyChanged(nameof(HasPendingCv));
        OnPropertyChanged(nameof(PendingCvDisplayName));
    }

    public void Reset()
    {
        JobName = string.Empty;
        CompanyName = string.Empty;
        JobSummary = null;
        JobUrl = null;
        AppliedOn = _dateProvider.Today;
        PendingCvSourcePath = null;
        PendingCvFileName = null;
        ValidationMessage = null;
        ErrorMessage = null;
        OnPropertyChanged(nameof(AppliedOnDisplay));
        OnPropertyChanged(nameof(HasPendingCv));
        OnPropertyChanged(nameof(PendingCvDisplayName));
    }

    partial void OnAppliedOnChanged(DateOnly value)
    {
        OnPropertyChanged(nameof(AppliedOnDisplay));
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
