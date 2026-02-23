using ApplyMate.App.ViewModels;
using ApplyMate.App.Services.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ApplyMate.App.Views.Pages;

public sealed partial class ApplicationsPage : Page
{
    private readonly ApplicationsViewModel _viewModel;

    public ApplicationsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ApplicationsViewModel>();
        DataContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var enableCheckEmailFilter = e.Parameter is string parameter &&
                                     string.Equals(
                                         parameter,
                                         NotificationRouteConstants.CheckEmailFilter,
                                         StringComparison.OrdinalIgnoreCase);

        _viewModel.SetCheckEmailOnlyMode(enableCheckEmailFilter);
        await _viewModel.LoadAsync(CancellationToken.None);
    }

    private void OnApplicationClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ApplicationListItemViewModel item)
        {
            _viewModel.OpenApplicationCommand.Execute(item);
        }
    }
}
