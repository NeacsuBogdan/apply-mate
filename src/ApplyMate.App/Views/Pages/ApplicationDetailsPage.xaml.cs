using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace ApplyMate.App.Views.Pages;

public sealed partial class ApplicationDetailsPage : Page
{
    public ApplicationDetailsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ApplicationDetailsViewModel>();
    }
}
