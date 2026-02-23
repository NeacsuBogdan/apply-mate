using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ApplyMate.App.Views.Pages;

public sealed partial class ApplicationDetailsPage : Page
{
    private readonly ApplicationDetailsViewModel _viewModel;

    public ApplicationDetailsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ApplicationDetailsViewModel>();
        DataContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid appId)
        {
            await _viewModel.LoadAsync(appId, CancellationToken.None);
        }
    }

    private async void OnAttachCvClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".pdf");
        picker.FileTypeFilter.Add(".doc");
        picker.FileTypeFilter.Add(".docx");
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var window = App.CurrentWindow;
        if (window is null)
        {
            return;
        }

        var windowHandle = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, windowHandle);

        var selectedFile = await picker.PickSingleFileAsync();
        if (selectedFile is null || string.IsNullOrWhiteSpace(selectedFile.Path))
        {
            return;
        }

        await _viewModel.AttachCvFromPathAsync(selectedFile.Path, CancellationToken.None);
    }
}
