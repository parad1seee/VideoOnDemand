using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace VideoOnDemand.Services.Services
{
    public class CallService : ICallService
    {
        private ITwillioService _twillioService;

        public CallService(ITwillioService twillioService)
        {
            _twillioService = twillioService;
        }
        public async Task VerificationCall(ApplicationUser user)
        {
            var call = (CallResource)await _twillioService.Call(user.PhoneNumber);

            var test = call.Sid;
        }
    }
}
