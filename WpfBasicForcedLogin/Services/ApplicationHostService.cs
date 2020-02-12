using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Extensions.Msal;

using WpfBasicForcedLogin.Contracts.Services;
using WpfBasicForcedLogin.Contracts.Views;
using WpfBasicForcedLogin.Core.Contracts.Services;
using WpfBasicForcedLogin.Models;
using WpfBasicForcedLogin.Strings;
using WpfBasicForcedLogin.ViewModels;

namespace WpfBasicForcedLogin.Services
{
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;
        private readonly IPersistAndRestoreService _persistAndRestoreService;
        private readonly IThemeSelectorService _themeSelectorService;
        private readonly IIdentityService _identityService;
        private readonly IUserDataService _userDataService;
        private readonly AppConfig _config;

        private IShellWindow _shellWindow;
        private ILogInWindow _logInWindow;

        public ApplicationHostService(IServiceProvider serviceProvider, INavigationService navigationService, IThemeSelectorService themeSelectorService, IPersistAndRestoreService persistAndRestoreService, IIdentityService identityService, IUserDataService userDataService, IOptions<AppConfig> config)
        {
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;
            _themeSelectorService = themeSelectorService;
            _persistAndRestoreService = persistAndRestoreService;
            _identityService = identityService;
            _userDataService = userDataService;
            _config = config.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Initialize services that you need before app activation
            await InitializeAsync();

            // https://aka.ms/msal-net-token-cache-serialization
            // this generates a file-system safe directory name to stick the cache in - or use something like Resources.AppDisplayName
            // should also consider this root dir for other configuration data
            var safeAppDirectoryName = System.IO.Path.GetInvalidFileNameChars().Aggregate(typeof(App).Assembly.GetName().Name, (current, c) => current.Replace(c, '-'));
            var rootCacheDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), safeAppDirectoryName, _config.IdentityCacheDirectoryName);
            var storageCreationProperties = new StorageCreationPropertiesBuilder(_config.IdentityCacheFileName, rootCacheDirectory, _config.IdentityClientId).Build();
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties).ConfigureAwait(false);
            _identityService.InitializeWithAadAndPersonalMsAccounts(_config.IdentityClientId, "http://localhost", cacheHelper);
            var silentLoginSuccess = await _identityService.AcquireTokenSilentAsync();
            if (!silentLoginSuccess || !_identityService.IsAuthorized())
            {
                _logInWindow = _serviceProvider.GetService(typeof(ILogInWindow)) as ILogInWindow;
                _logInWindow.ShowWindow();
                await StartupAsync();
                return;
            }

            _shellWindow = _serviceProvider.GetService(typeof(IShellWindow)) as IShellWindow;
            _navigationService.Initialize(_shellWindow.GetNavigationFrame());
            _shellWindow.ShowWindow();
            _navigationService.NavigateTo(typeof(MainViewModel).FullName);

            // Tasks after activation
            await StartupAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _persistAndRestoreService.PersistData();
            _identityService.LoggedIn -= OnLoggedIn;
            _identityService.LoggedOut -= OnLoggedOut;
        }

        private async Task InitializeAsync()
        {
            await Task.CompletedTask;
            _persistAndRestoreService.RestoreData();
            _themeSelectorService.SetTheme();
            _userDataService.Initialize();
            _identityService.LoggedIn += OnLoggedIn;
            _identityService.LoggedOut += OnLoggedOut;
        }

        private async Task StartupAsync()
        {
            await Task.CompletedTask;
        }

        private void OnLoggedIn(object sender, EventArgs e)
        {
            _shellWindow = _serviceProvider.GetService(typeof(IShellWindow)) as IShellWindow;
            _navigationService.Initialize(_shellWindow.GetNavigationFrame());
            _shellWindow.ShowWindow();
            _navigationService.NavigateTo(typeof(MainViewModel).FullName);
            _logInWindow.CloseWindow();
        }

        private void OnLoggedOut(object sender, EventArgs e)
        {
            // Show the LogIn Window
            _logInWindow = _serviceProvider.GetService(typeof(ILogInWindow)) as ILogInWindow;
            _logInWindow.ShowWindow();

            // Close the Shell Window and
            _shellWindow.CloseWindow();
            _navigationService.UnsubscribeNavigation();
        }
    }
}
