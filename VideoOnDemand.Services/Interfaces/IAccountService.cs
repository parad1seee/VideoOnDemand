using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface IAccountService
    {
        Task<TokenResponseModel> RefreshTokenAsync(string refreshToken, List<string> roles);

        Task<RegisterResponseModel> Register(RegisterRequestModel model);

        Task<RegisterUsingPhoneResponseModel> RegisterUsingPhone(RegisterUsingPhoneRequestModel model);

        Task<LoginResponseModel> ConfirmEmail(ConfirmEmailRequestModel model);

        Task<LoginResponseModel> ConfirmPhone(ConfirmPhoneRequestModel model);

        Task<LoginResponseModel> Login(LoginRequestModel model);

        Task<LoginResponseModel> LoginUsingPhone(LoginWithPhoneRequestModel model);

        Task<LoginResponseModel> AdminLogin(AdminLoginRequestModel model);

        Task Logout(int userId);

        Task SendPasswordRestorationLink(string email, bool isAdmin = false);

        Task SendConfirmEmailLink(ApplicationUser user);

        Task<LoginResponseModel> ResetPassword(ResetPasswordRequestModel model, bool isAdmin = false);

        Task<LoginResponseModel> ResetPassword(ResetPasswordWithPhoneRequestModel model);

        Task ChangePassword(ChangePasswordRequestModel model, int userId);

        Task SendPasswordRestorationCodeAsync(string phoneNumber);
    }
}
