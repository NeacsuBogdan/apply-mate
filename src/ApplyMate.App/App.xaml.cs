using ApplyMate.App.Navigation;
using ApplyMate.App.Services.Settings;
using ApplyMate.App.ViewModels;
using ApplyMate.Core.Abstractions;
using ApplyMate.Core.Services;
using ApplyMate.Infrastructure.Persistence;
using ApplyMate.Infrastructure.Persistence.Pathing;
using ApplyMate.Infrastructure.Repositories;
using ApplyMate.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace ApplyMate.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var databaseInitializer = Services.GetRequiredService<IApplyMateDatabaseInitializer>();
        databaseInitializer.EnsureCreatedAsync(CancellationToken.None).GetAwaiter().GetResult();

        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        RegisterCore(services);
        RegisterInfrastructure(services);
        RegisterApp(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterCore(IServiceCollection services)
    {
        services.AddSingleton<IDateProvider, SystemDateProvider>();
        services.AddSingleton<ISettingsStore, InMemorySettingsStore>();
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddSingleton<ILocalStoragePathProvider, WindowsLocalStoragePathProvider>();
        services.AddSingleton<IDatabasePathProvider, SqliteDatabasePathProvider>();
        services.AddSingleton<IDbContextFactory<ApplyMateDbContext>, SqliteDbContextFactory>();
        services.AddSingleton<IApplyMateDatabaseInitializer, ApplyMateDatabaseInitializer>();

        services.AddTransient<IJobApplicationRepository, JobApplicationRepository>();
        services.AddTransient<ICvStorageService, CvStorageService>();
    }

    private static void RegisterApp(IServiceCollection services)
    {
        services.AddSingleton<PageRegistry>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddTransient<MainWindow>();
        services.AddTransient<MainShellViewModel>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ApplicationsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AddApplicationViewModel>();
        services.AddTransient<ApplicationDetailsViewModel>();
    }
}
