using ApplyMate.App.Navigation;
using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace ApplyMate.App.Views;

public sealed partial class MainShellPage : Page
{
    private readonly MainShellViewModel _viewModel;
    private readonly INavigationService _navigationService;

    public MainShellPage()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<MainShellViewModel>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        DataContext = _viewModel;

        _navigationService.AttachFrame(ContentFrame);
        _navigationService.NavigateTo(PageKeys.Dashboard, clearBackStack: true);

        RootNavigationView.BackRequested += OnBackRequested;
    }

    private void OnSelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string pageKey)
        {
            return;
        }

        _viewModel.NavigateCommand.Execute(pageKey);
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        _navigationService.GoBack();
    }
}
