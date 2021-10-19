using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Socials;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface IGoogleService
    {
        Task<GProfileResponseModel> GetProfile(string token);

        Task<LoginResponseModel> ProcessRequest(GoogleWithPhoneRequestModel model);

        Task<LoginResponseModel> ProcessRequest(GoogleWithEmailRequestModel model);

        Task<LoginResponseModel> ConfrimRegistration(ConfirmPhoneRequestModel model);
    }
}
