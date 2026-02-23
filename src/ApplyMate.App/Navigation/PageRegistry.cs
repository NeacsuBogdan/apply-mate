using ApplyMate.App.Views.Pages;

namespace ApplyMate.App.Navigation;

public sealed class PageRegistry
{
    private readonly Dictionary<string, Type> _pagesByKey = new(StringComparer.OrdinalIgnoreCase);

    public PageRegistry()
    {
        Register<DashboardPage>(PageKeys.Dashboard);
        Register<ApplicationsPage>(PageKeys.Applications);
        Register<SettingsPage>(PageKeys.Settings);
        Register<AddApplicationPage>(PageKeys.AddApplication);
        Register<ApplicationDetailsPage>(PageKeys.ApplicationDetails);
    }

    public Type Resolve(string pageKey)
    {
        if (_pagesByKey.TryGetValue(pageKey, out var pageType))
        {
            return pageType;
        }

        throw new InvalidOperationException($"Page key '{pageKey}' is not registered.");
    }

    public void Register<TPage>(string pageKey)
    {
        _pagesByKey[pageKey] = typeof(TPage);
    }
}
