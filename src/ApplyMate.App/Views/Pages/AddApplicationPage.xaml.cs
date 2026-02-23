using ApplyMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

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

        _viewModel.SetCvFromPath(selectedFile.Path);
    }
}
