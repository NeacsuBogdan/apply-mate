using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
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
