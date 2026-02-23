using System.Collections.ObjectModel;
using ApplyMate.App.Messaging;
using ApplyMate.App.Navigation;
using ApplyMate.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace ApplyMate.App.ViewModels;

public sealed partial class ApplicationsViewModel : ViewModelBase, IRecipient<NoResponseRuleAppliedMessage>
{
    private readonly IJobApplicationRepository _repository;
    private readonly INavigationService _navigationService;

    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchDebounceCts;

    public ApplicationsViewModel(
        IJobApplicationRepository repository,
        INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Applications = new ObservableCollection<ApplicationListItemViewModel>();
        StatusOptions = ApplicationStatusOption.FilterOptions;
        SelectedStatus = StatusOptions[0];

        WeakReferenceMessenger.Default.Register(this);
    }

    public ObservableCollection<ApplicationListItemViewModel> Applications { get; }

    public IReadOnlyList<ApplicationStatusOption> StatusOptions { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ApplicationStatusOption _selectedStatus;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private void AddApplication()
    {
        _navigationService.NavigateTo(PageKeys.AddApplication);
    }

    [RelayCommand]
    private void OpenApplication(ApplicationListItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _navigationService.NavigateTo(PageKeys.ApplicationDetails, item.Id);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        await LoadInternalAsync(_loadCts.Token);
    }

    public void Receive(NoResponseRuleAppliedMessage message)
    {
        _ = RefreshAsync();
    }

    public Task LoadAsync(CancellationToken ct)
    {
        return LoadInternalAsync(ct);
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = DebounceAndRefreshAsync();
    }

    partial void OnSelectedStatusChanged(ApplicationStatusOption value)
    {
        _ = RefreshAsync();
    }

    private async Task DebounceAndRefreshAsync()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), _searchDebounceCts.Token);
            await RefreshAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadInternalAsync(CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var apps = await _repository.GetAllAsync(
                SearchText,
                SelectedStatus.Value,
                ct);

            Applications.Clear();
            foreach (var app in apps)
            {
                Applications.Add(new ApplicationListItemViewModel(app));
            }
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
}
