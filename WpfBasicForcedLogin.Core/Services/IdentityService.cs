﻿using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

using WpfBasicForcedLogin.Core.Contracts.Services;
using WpfBasicForcedLogin.Core.Helpers;

namespace WpfBasicForcedLogin.Core.Services
{
    public class IdentityService : IIdentityService
    {
        //// For more information about using Identity, see
        //// https://github.com/microsoft/WindowsTemplateStudio/blob/master/docs/WPF/services/identity.md
        ////
        //// Read more about Microsoft Identity Client here
        //// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki
        //// https://docs.microsoft.com/azure/active-directory/develop/v2-overview

        // TODO WTS: The IdentityClientId in App.config is provided to test the project in development environments.
        // Please, follow these steps to create a new one with Azure Active Directory and replace it before going to production.
        // https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app
        private readonly string[] _graphScopes = new string[] { "user.read" };
        private bool _integratedAuthAvailable;
        private IPublicClientApplication _client;
        private AuthenticationResult _authenticationResult;

        public event EventHandler LoggedIn;

        public event EventHandler LoggedOut;

        public IdentityService()
        {
        }

        public void InitializeWithAadAndPersonalMsAccounts(string clientId, string redirectUri = null, MsalCacheHelper cacheHelper = null)
        {
            _integratedAuthAvailable = false;
            _client = PublicClientApplicationBuilder.Create(clientId)
                                                    .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();

            cacheHelper?.RegisterCache(_client.UserTokenCache);
        }

        public void InitializeWithAadMultipleOrgs(string clientId, bool integratedAuth = false, string redirectUri = null, MsalCacheHelper cacheHelper = null)
        {
            _integratedAuthAvailable = integratedAuth;
            _client = PublicClientApplicationBuilder.Create(clientId)
                                                    .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();
            cacheHelper?.RegisterCache(_client.UserTokenCache);
        }

        public void InitializeWithAadSingleOrg(string clientId, string tenant, bool integratedAuth = false, string redirectUri = null, MsalCacheHelper cacheHelper = null)
        {
            _integratedAuthAvailable = integratedAuth;
            _client = PublicClientApplicationBuilder.Create(clientId)
                                                    .WithAuthority(AzureCloudInstance.AzurePublic, tenant)
                                                    .WithRedirectUri(redirectUri)
                                                    .Build();

            cacheHelper?.RegisterCache(_client.UserTokenCache);
        }

        public bool IsLoggedIn() => _authenticationResult != null;

        public async Task<LoginResultType> LoginAsync()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return LoginResultType.NoNetworkAvailable;
            }

            try
            {
                var accounts = await _client.GetAccountsAsync();
                _authenticationResult = await _client.AcquireTokenInteractive(_graphScopes)
                                                     .WithAccount(accounts.FirstOrDefault())
                                                     .ExecuteAsync();
                if (!IsAuthorized())
                {
                    _authenticationResult = null;
                    return LoginResultType.Unauthorized;
                }

                LoggedIn?.Invoke(this, EventArgs.Empty);
                return LoginResultType.Success;
            }
            catch (MsalClientException ex)
            {
                if (ex.ErrorCode == "authentication_canceled")
                {
                    return LoginResultType.CancelledByUser;
                }

                return LoginResultType.UnknownError;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return LoginResultType.UnknownError;
            }
        }

        public bool IsAuthorized()
        {
            // TODO WTS: You can also add extra authorization checks here.
            // i.e.: Checks permisions of _authenticationResult.Account.Username in a database.
            return true;
        }

        public string GetAccountUserName()
        {
            return _authenticationResult?.Account?.Username;
        }

        public async Task LogoutAsync()
        {
            try
            {
                var accounts = await _client.GetAccountsAsync();
                var account = accounts.FirstOrDefault();
                if (account != null)
                {
                    await _client.RemoveAsync(account);
                }

                _authenticationResult = null;
                LoggedOut?.Invoke(this, EventArgs.Empty);
            }
            catch (MsalException)
            {
                // TODO WTS: LogoutAsync can fail please handle exceptions as appropriate to your scenario
                // For more info on MsalExceptions see
                // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/exceptions
            }
        }

        public async Task<string> GetAccessTokenForGraphAsync() => await GetAccessTokenAsync(_graphScopes);

        public async Task<string> GetAccessTokenAsync(string[] scopes)
        {
            var acquireTokenSuccess = await AcquireTokenSilentAsync(scopes);
            if (acquireTokenSuccess)
            {
                return _authenticationResult.AccessToken;
            }
            else
            {
                try
                {
                    // Interactive authentication is required
                    var accounts = await _client.GetAccountsAsync();
                    _authenticationResult = await _client.AcquireTokenInteractive(scopes)
                                                         .WithAccount(accounts.FirstOrDefault())
                                                         .ExecuteAsync();
                    return _authenticationResult.AccessToken;
                }
                catch (MsalException)
                {
                    // AcquireTokenSilent and AcquireTokenInteractive failed, the session will be closed.
                    _authenticationResult = null;
                    LoggedOut?.Invoke(this, EventArgs.Empty);
                    return string.Empty;
                }
            }
        }

        public async Task<bool> AcquireTokenSilentAsync() => await AcquireTokenSilentAsync(_graphScopes);

        private async Task<bool> AcquireTokenSilentAsync(string[] scopes)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return false;
            }

            try
            {
                var accounts = await _client.GetAccountsAsync();
                _authenticationResult = await _client.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                                                     .ExecuteAsync();
                return true;
            }
            catch (MsalUiRequiredException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                if (_integratedAuthAvailable)
                {
                    try
                    {
                        _authenticationResult = await _client.AcquireTokenByIntegratedWindowsAuth(_graphScopes)
                                                             .ExecuteAsync();
                        return true;
                    }
                    catch (MsalUiRequiredException)
                    {
                        // Interactive authentication is required
                        return false;
                    }
                }
                else
                {
                    // Interactive authentication is required
                    return false;
                }
            }
            catch (MsalException)
            {
                // TODO WTS: Silentauth failed, please handle this exception as appropriate to your scenario
                // For more info on MsalExceptions see
                // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/exceptions
                return false;
            }
        }

        private void ConfigureCache(MsalCacheHelper cacheHelper)
        {
            if (cacheHelper != null)
            {
                // https://aka.ms/msal-net-token-cache-serialization
                cacheHelper.RegisterCache(_client.UserTokenCache);
            }
        }
    }
}
