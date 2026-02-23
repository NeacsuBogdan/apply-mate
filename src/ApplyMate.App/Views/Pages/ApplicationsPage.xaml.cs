using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace ApplyMate.App.Views.Pages;

public sealed partial class ApplicationsPage : Page
{
    public ApplicationsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ApplicationsViewModel>();
    }
}
