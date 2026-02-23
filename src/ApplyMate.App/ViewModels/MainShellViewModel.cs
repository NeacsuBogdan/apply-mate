using ApplyMate.App.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Navigation;

namespace ApplyMate.App.ViewModels;

public sealed partial class MainShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public MainShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.Navigated += OnNavigated;
        SelectedPageKey = PageKeys.Dashboard;
    }

    [ObservableProperty]
    private string _selectedPageKey;

    [RelayCommand]
    public void Navigate(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            return;
        }

        _navigationService.NavigateTo(pageKey);
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        var sourceName = e.SourcePageType.Name;
        SelectedPageKey = sourceName switch
        {
            "DashboardPage" => PageKeys.Dashboard,
            "ApplicationsPage" => PageKeys.Applications,
            "SettingsPage" => PageKeys.Settings,
            "AddApplicationPage" => PageKeys.AddApplication,
            "ApplicationDetailsPage" => PageKeys.ApplicationDetails,
            _ => SelectedPageKey
        };
    }
}
