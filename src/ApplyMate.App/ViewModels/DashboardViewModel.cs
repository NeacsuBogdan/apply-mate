using System.Collections.ObjectModel;
using ApplyMate.App.Messaging;
using ApplyMate.App.Navigation;
using ApplyMate.Core.Abstractions;
using ApplyMate.Core.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace ApplyMate.App.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase, IRecipient<NoResponseRuleAppliedMessage>
{
    private readonly IJobApplicationRepository _repository;
    private readonly IDateProvider _dateProvider;
    private readonly INavigationService _navigationService;

    private CancellationTokenSource? _loadCts;

    public DashboardViewModel(
        IJobApplicationRepository repository,
        IDateProvider dateProvider,
        INavigationService navigationService)
    {
        _repository = repository;
        _dateProvider = dateProvider;
        _navigationService = navigationService;

        UpcomingInterviews = new ObservableCollection<JobApplication>();
        RecentApplications = new ObservableCollection<JobApplication>();

        WeakReferenceMessenger.Default.Register(this);
    }

    [ObservableProperty]
    private int _total;

    [ObservableProperty]
    private int _applied;

    [ObservableProperty]
    private int _inProgress;

    [ObservableProperty]
    private int _noResponse;

    [ObservableProperty]
    private int _rejected;

    [ObservableProperty]
    private int _accepted;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<JobApplication> UpcomingInterviews { get; }

    public ObservableCollection<JobApplication> RecentApplications { get; }

    [RelayCommand]
    private void AddApplication()
    {
        _navigationService.NavigateTo(PageKeys.AddApplication);
    }

    [RelayCommand]
    private void OpenApplication(JobApplication? application)
    {
        if (application is null)
        {
            return;
        }

        _navigationService.NavigateTo(PageKeys.ApplicationDetails, application.Id);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        await LoadAsync(_loadCts.Token);
    }

    public void Receive(NoResponseRuleAppliedMessage message)
    {
        _ = RefreshAsync();
    }

    public async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var counts = await _repository.GetDashboardCountsAsync(ct);
            Total = counts.Total;
            Applied = counts.Applied;
            InProgress = counts.InProgress;
            NoResponse = counts.NoResponse;
            Rejected = counts.Rejected;
            Accepted = counts.Accepted;

            var localToday = _dateProvider.Today.ToDateTime(TimeOnly.MinValue);
            var from = new DateTimeOffset(localToday);
            var to = from.AddDays(7).AddDays(1).AddTicks(-1);

            var upcoming = await _repository.GetUpcomingInterviewsAsync(from, to, ct);
            Replace(UpcomingInterviews, upcoming);

            var recent = await _repository.GetRecentAsync(10, ct);
            Replace(RecentApplications, recent);
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

    private static void Replace(
        ObservableCollection<JobApplication> target,
        IReadOnlyList<JobApplication> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
