using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Socials;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface IFacebookService
    {
        Task<FBProfileResponseModel> GetProfile(string token);

        Task<LoginResponseModel> ProcessRequest(FacebookWithPhoneRequestModel model);

        Task<LoginResponseModel> ProcessRequest(FacebookWithEmailRequestModel model);

        Task<LoginResponseModel> ConfirmFacebookRegistration(ConfirmPhoneRequestModel model);
    }
}
