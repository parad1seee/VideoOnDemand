using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Socials;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface ILinkedInService
    {
        Task<LIProfileResponseModel> GetProfile(string token, string redirectUri, bool emailRequest = false);

        Task<LoginResponseModel> ProcessRequest(LinkedInWithEmailRequestModel model);

        Task<LoginResponseModel> ProcessRequest(LinkedInWithPhoneRequestModel model);

        Task<LoginResponseModel> ConfrimRegistration(ConfirmPhoneRequestModel model);
    }
}
