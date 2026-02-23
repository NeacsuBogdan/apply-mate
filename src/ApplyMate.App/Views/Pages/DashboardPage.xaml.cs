using ApplyMate.App.ViewModels;
using ApplyMate.Core.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ApplyMate.App.Views.Pages;

public sealed partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<DashboardViewModel>();
        DataContext = _viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync(CancellationToken.None);
    }

    private void OnUpcomingInterviewClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is JobApplication application)
        {
            _viewModel.OpenApplicationCommand.Execute(application);
        }
    }

    private void OnRecentApplicationClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is JobApplication application)
        {
            _viewModel.OpenApplicationCommand.Execute(application);
        }
    }
}
