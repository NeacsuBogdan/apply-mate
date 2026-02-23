using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace ApplyMate.App.Views.Pages;

public sealed partial class AddApplicationPage : Page
{
    public AddApplicationPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<AddApplicationViewModel>();
    }
}
