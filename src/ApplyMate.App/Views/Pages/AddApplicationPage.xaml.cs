using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ApplyMate.App.Views.Pages;

public sealed partial class AddApplicationPage : Page
{
    private readonly AddApplicationViewModel _viewModel;

    public AddApplicationPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<AddApplicationViewModel>();
        DataContext = _viewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Reset();
    }
}
