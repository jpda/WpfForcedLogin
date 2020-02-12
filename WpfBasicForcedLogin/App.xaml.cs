using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WpfBasicForcedLogin.Contracts.Services;
using WpfBasicForcedLogin.Contracts.Views;
using WpfBasicForcedLogin.Core.Contracts.Services;
using WpfBasicForcedLogin.Core.Services;
using WpfBasicForcedLogin.Models;
using WpfBasicForcedLogin.Services;
using WpfBasicForcedLogin.ViewModels;
using WpfBasicForcedLogin.Views;
using Graph = Microsoft.Graph;

namespace WpfBasicForcedLogin
{
    // For more inforation about application lifecyle events see https://docs.microsoft.com/dotnet/framework/wpf/app-development/application-management-overview
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
        }

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // For more information about .NET generic host see  https://docs.microsoft.com/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.0
            _host = Host.CreateDefaultBuilder(e.Args)
                    .ConfigureAppConfiguration(c => c.SetBasePath(appLocation))
                    .ConfigureServices(ConfigureServices)
                    .Build();

            
            await _host.StartAsync();
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // TODO WTS: Register your services, viewmodels and pages here

            // App Host
            services.AddHostedService<ApplicationHostService>();

            // Core Services

            // using http for graph
            //services.AddHttpClient("msgraph", x =>
            //{
            //    x.BaseAddress = new System.Uri("https://graph.microsoft.com/v1.0/");
            //});
            //services.AddSingleton<IMicrosoftGraphService, MicrosoftGraphService>();

            services.AddSingleton<IIdentityService, IdentityService>();

            // using graph client
            services.AddSingleton<Graph.IAuthenticationProvider, IdentityServiceGraphTokenProvider>();
            services.AddSingleton<Graph.IGraphServiceClient, Graph.GraphServiceClient>();
            services.AddSingleton<IMicrosoftGraphService, MicrosoftGraphClientService>();

            services.AddSingleton<IFileService, FileService>();

            // Services
            services.AddSingleton<IPersistAndRestoreService, PersistAndRestoreService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<ISampleDataService, SampleDataService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Views and ViewModels
            services.AddTransient<IShellWindow, ShellWindow>();
            services.AddTransient<ShellViewModel>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();

            services.AddTransient<MasterDetailViewModel>();
            services.AddTransient<MasterDetailPage>();

            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();

            services.AddSingleton<IUserDataService, UserDataService>();
            services.AddTransient<ILogInWindow, LogInWindow>();
            services.AddTransient<LogInViewModel>();

            // Configuration
            services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
        }

        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // TODO WTS: Please handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/dotnet/api/system.windows.application.dispatcherunhandledexception?view=netcore-3.0

            // e.Handled = true;
        }
    }
}
