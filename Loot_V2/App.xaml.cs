using Loot_V2.Activation;
using Loot_V2.Contracts.Services;
using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Services;
using Loot_V2.Helpers;
using Loot_V2.Models;
using Loot_V2.Services;
using Loot_V2.ViewModels;
using Loot_V2.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace Loot_V2;

public partial class App : Application
{
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Activation
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Infrastructure services
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core services (platform-agnostic)
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IBudgetService, BudgetService>();
            services.AddSingleton<IMonthDataService, MonthDataService>();
            services.AddSingleton<IOFXImportService, OFXImportService>();
            services.AddSingleton<IMatchingService, MatchingService>();

            // Plaid service is retained but not registered — may be used in the future
            // services.AddPlaid(context.Configuration.GetSection("Plaid"));
            // services.AddSingleton<IPlaidService, PlaidService>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<BudgetViewModel>();
            services.AddTransient<BudgetPage>();
            services.AddTransient<ImportsViewModel>();
            services.AddTransient<ImportsPage>();
            services.AddTransient<TransactionsViewModel>();
            services.AddTransient<TransactionsPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
