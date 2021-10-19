using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface ISMSService
    {
        Task<bool> SendVerificationCodeAsync(ApplicationUser user, string phoneNumber, VerificationCodeType type, string data = null);

        Task<bool> SendVerificationCodeAsync(string phoneNumber, VerificationCodeType type, string data = null);
    }
}
