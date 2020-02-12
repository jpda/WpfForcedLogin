using System.Threading.Tasks;
using GraphClient = Microsoft.Graph;
using WpfBasicForcedLogin.Core.Contracts.Services;
using System.Net.Http;

namespace WpfBasicForcedLogin.Core.Services
{
    public class IdentityServiceGraphTokenProvider : GraphClient.IAuthenticationProvider
    {
        private readonly IIdentityService _identityService;

        public IdentityServiceGraphTokenProvider(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var token = await _identityService.GetAccessTokenForGraphAsync();
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}
