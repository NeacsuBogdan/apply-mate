using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ApplyMate.App.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly PageRegistry _pageRegistry;
    private Frame? _frame;

    public NavigationService(PageRegistry pageRegistry)
    {
        _pageRegistry = pageRegistry;
    }

    public event EventHandler<NavigationEventArgs>? Navigated;

    public void AttachFrame(Frame frame)
    {
        _frame = frame;
        _frame.Navigated -= OnNavigated;
        _frame.Navigated += OnNavigated;
    }

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearBackStack = false)
    {
        if (_frame is null)
        {
            return false;
        }

        var pageType = _pageRegistry.Resolve(pageKey);
        var didNavigate = _frame.Navigate(pageType, parameter);

        if (didNavigate && clearBackStack)
        {
            _frame.BackStack.Clear();
        }

        return didNavigate;
    }

    public bool GoBack()
    {
        if (_frame is null || !_frame.CanGoBack)
        {
            return false;
        }

        _frame.GoBack();
        return true;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        Navigated?.Invoke(this, e);
    }
}
