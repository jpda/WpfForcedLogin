using System;
using System.Threading.Tasks;
using GraphClient = Microsoft.Graph;
using WpfBasicForcedLogin.Core.Contracts.Services;
using WpfBasicForcedLogin.Core.Models;
using System.Linq;
using WpfBasicForcedLogin.Core.Helpers;

namespace WpfBasicForcedLogin.Core.Services
{
    public class MicrosoftGraphClientService : IMicrosoftGraphService
    {
        private readonly GraphClient.IGraphServiceClient _client;
        public MicrosoftGraphClientService(GraphClient.IGraphServiceClient client)
        {
            _client = client;
        }

        public async Task<User> GetUserInfoAsync(string accessToken)
        {
            var meRequest = _client.Me.Request();
            var me = await meRequest.GetAsync();
            return new User()
            {
                BusinessPhones = me.BusinessPhones.ToList(),
                DisplayName = me.DisplayName,
                GivenName = me.GivenName,
                Id = me.Id,
                JobTitle = me.JobTitle,
                Mail = me.Mail,
                MobilePhone = me.MobilePhone,
                OfficeLocation = me.OfficeLocation,
                PreferredLanguage = me.PreferredLanguage,
                Surname = me.Surname,
                UserPrincipalName = me.UserPrincipalName
            };

        }

        public async Task<string> GetUserPhoto(string accessToken)
        {
            var meRequest = _client.Me.Photo.Content.Request();
            var photo = await meRequest.GetAsync();
            return photo.ToBase64String();
        }
    }
}
