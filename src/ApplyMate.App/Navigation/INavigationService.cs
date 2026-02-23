using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ApplyMate.App.Navigation;

public interface INavigationService
{
    event EventHandler<NavigationEventArgs>? Navigated;

    void AttachFrame(Frame frame);

    bool NavigateTo(string pageKey, object? parameter = null, bool clearBackStack = false);

    bool GoBack();
}
