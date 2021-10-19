using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Common.Utilities;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Domain.Extentions;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.InternalModels;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.ScheduledTasks;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Jobs;
using VideoOnDemand.WebSockets.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Web;

namespace VideoOnDemand.Services.Services
{
    public class AccountService : IAccountService
    {
        private UserManager<ApplicationUser> _userManager;
        private HashUtility _hashUtility;
        private IUnitOfWork _unitOfWork;
        private ISMSService _smsService;
        private IEmailService _emailService;
        private IJWTService _jwtService;
        private IHttpContextAccessor _httpContextAccessor;
        private OneWeekAfterRegistration _weekAfterRegistration;
        private WebSocketMessageHandler _webSocketHandler;

        public AccountService(UserManager<ApplicationUser> userManager, HashUtility hashUtility, IUnitOfWork unitOfWork, IEmailService emailService, ISMSService smsService, IJWTService jwtService,
            IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider, WebSocketMessageHandler webSocketHandler)
        {
            _userManager = userManager;
            _hashUtility = hashUtility;
            _unitOfWork = unitOfWork;
            _smsService = smsService;
            _emailService = emailService;
            _jwtService = jwtService;
            _httpContextAccessor = httpContextAccessor;
            _webSocketHandler = webSocketHandler;

            // TODO: Remove if not used ScheduledTasks
            _weekAfterRegistration = serviceProvider.GetScheduledTask<OneWeekAfterRegistration>();
        }

        public async Task<RegisterResponseModel> Register(RegisterRequestModel model)
        {
            model.Email = model.Email.Trim().ToLower();

            ApplicationUser user = _unitOfWork.Repository<ApplicationUser>().Find(x => x.Email.ToLower() == model.Email);

            if (user != null && user.EmailConfirmed)
                throw new CustomException(HttpStatusCode.UnprocessableEntity, "email", "Email is already registered");

            if (user == null)
            {
                user = new ApplicationUser
                {
                    Email = model.Email,
                    UserName = model.Email,
                    IsActive = true,
                    RegistratedAt = DateTime.UtcNow,
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                    throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

                result = await _userManager.AddToRoleAsync(user, Role.User);

                if (!result.Succeeded)
                    throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

                // TODO: Remove if not used ScheduledTasks
                _weekAfterRegistration.Add(user.Id, DateTime.UtcNow);
            }

            try
            {
                await SendConfirmEmailLink(user);
            }
            catch (Exception ex)
            {
                await _userManager.DeleteAsync(user);
                throw;
            }

            return new RegisterResponseModel { Email = user.Email };
        }

        public async Task<RegisterUsingPhoneResponseModel> RegisterUsingPhone(RegisterUsingPhoneRequestModel model)
        {
            if (_unitOfWork.Repository<ApplicationUser>().Find(x => x.PhoneNumberConfirmed && x.PhoneNumber == model.PhoneNumber) != null)
                throw new CustomException(HttpStatusCode.UnprocessableEntity, "phone", "Phone is already registered");

            bool smsSent = false;
            try
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(model.Password);
                var password = Convert.ToBase64String(plainTextBytes);

                var data = JsonConvert.SerializeObject(new RegisterWithPhoneInternalModel
                {
                    Phone = model.PhoneNumber,
                    Password = password
                }, new JsonSerializerSettings { Formatting = Formatting.Indented });

                smsSent = await _smsService.SendVerificationCodeAsync(model.PhoneNumber, VerificationCodeType.Confirm, data);
            }
            catch (SystemException ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "general", "Error while sending message");
            }

            return new RegisterUsingPhoneResponseModel
            {
                Phone = model.PhoneNumber,
                SMSSent = smsSent
            };
        }

        public async Task<LoginResponseModel> ConfirmEmail(ConfirmEmailRequestModel model)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Find(x => x.Email == model.Email);

            var token = HttpUtility.UrlDecode(model.Token).Replace(" ", "+");

            if (user == null)
                throw new CustomException(HttpStatusCode.BadRequest, "email", "Email is invalid");

            user.ThrowIfBlocked();

            if (user.EmailConfirmed)
                throw new CustomException(HttpStatusCode.BadRequest, "email", "The email address has been already verified");

            // Confirm email
            var confirmResult = await _userManager.ConfirmEmailAsync(user, token);

            if (!confirmResult.Succeeded)
                throw new CustomException(HttpStatusCode.BadRequest, "token", "The verification link is not active");

            return await _jwtService.BuildLoginResponse(user);
        }

        public async Task<LoginResponseModel> ConfirmPhone(ConfirmPhoneRequestModel model)
        {
            var dbCode = _unitOfWork.Repository<VerificationToken>().Find(x => !x.IsUsed && x.IsValid && x.PhoneNumber == model.PhoneNumber && x.Type == VerificationCodeType.Confirm && x.TokenHash == _hashUtility.GetHash(model.Code));

            if (dbCode == null)
                throw new CustomException(HttpStatusCode.BadRequest, "general", "SMS code is not valid. Add correct code or re-send it");

            var userData = JsonConvert.DeserializeObject<RegisterWithPhoneInternalModel>(dbCode.Data);

            var base64EncodedBytes = Convert.FromBase64String(userData.Password);
            var password = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

            var user = new ApplicationUser
            {
                PhoneNumber = userData.Phone,
                UserName = userData.Phone,
                IsActive = true,
                RegistratedAt = DateTime.UtcNow,
                PhoneNumberConfirmed = true,
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

            result = await _userManager.AddToRoleAsync(user, Role.User);

            if (!result.Succeeded)
                throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

            dbCode.IsUsed = true;

            _unitOfWork.Repository<VerificationToken>().Update(dbCode);
            _unitOfWork.Repository<ApplicationUser>().Update(user);

            // there is _unitOfWork.SaveChanges call inside BuildLoginResponse
            return await _jwtService.BuildLoginResponse(user);
        }

        public async Task<LoginResponseModel> Login(LoginRequestModel model)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Email == model.Email)
                .Include(x => x.Profile.Avatar)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefault();

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password) || !user.UserRoles.Any(x => x.Role.Name == Role.User))
                throw new CustomException(HttpStatusCode.BadRequest, "credentials", "Invalid credentials");

            if (!string.IsNullOrEmpty(model.Email) && !user.EmailConfirmed)
                throw new CustomException(HttpStatusCode.BadRequest, "email", "Email is not confirmed");

            if (user.IsDeleted)
                throw new CustomException(HttpStatusCode.BadRequest, "general", "Your account was deleted by admin, to know more please contact administration.");

            if (!user.IsActive)
                throw new CustomException(HttpStatusCode.Forbidden, "general", "Your account was blocked, to know more please contact administration.");

            return await _jwtService.BuildLoginResponse(user, model.AccessTokenLifetime);
        }

        public async Task<LoginResponseModel> LoginUsingPhone(LoginWithPhoneRequestModel model)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().Get(x => x.PhoneNumber == model.PhoneNumber)
                .Include(x => x.Profile.Avatar)
                    .FirstOrDefaultAsync();

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                throw new CustomException(HttpStatusCode.BadRequest, "credentials", "Invalid credentials");

            if (!user.PhoneNumberConfirmed)
                throw new CustomException(HttpStatusCode.BadRequest, "phoneNumber", "PhoneNumber is not confirmed");

            if (user.IsDeleted)
                throw new CustomException(HttpStatusCode.BadRequest, "general", "Your account was deleted by admin, to know more please contact administration.");

            if (!user.IsActive)
                throw new CustomException(HttpStatusCode.MethodNotAllowed, "general", "Your account was blocked, to know more please contact administration.");

            return await _jwtService.BuildLoginResponse(user, model.AccessTokenLifetime);
        }

        public async Task<LoginResponseModel> AdminLogin(AdminLoginRequestModel model)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Email == model.Email)
                .TagWith(nameof(Login) + "_GetAdmin")
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefault();

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password) || !user.UserRoles.Any(x => x.Role.Name == Role.Admin || x.Role.Name == Role.SuperAdmin))
                throw new CustomException(HttpStatusCode.BadRequest, "general", "Invalid credentials");

            return await _jwtService.BuildLoginResponse(user, model.AccessTokenLifetime);
        }

        public async Task<TokenResponseModel> RefreshTokenAsync(string refreshToken, List<string> roles)
        {
            var token = _unitOfWork.Repository<UserToken>().Get(w => w.RefreshTokenHash == _hashUtility.GetHash(refreshToken) && w.IsActive && w.RefreshExpiresDate > DateTime.UtcNow)
                .TagWith(nameof(RefreshTokenAsync) + "_GetRefreshToken")
                .Include(x => x.User)
                    .ThenInclude(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                .FirstOrDefault();

            if (token == null)
                throw new CustomException(HttpStatusCode.BadRequest, "refreshToken", "Refresh token is invalid");

            if (!token.User.UserRoles.Any(x => roles.Contains(x.Role.Name)))
                throw new CustomException(HttpStatusCode.Forbidden, "role", "Access denied");

            var result = await _jwtService.CreateUserTokenAsync(token.User, isRefresh: true);
            _unitOfWork.SaveChanges();

            await _webSocketHandler.RemoveWebSocketConnectionByUserId(token.UserId);

            return result;
        }

        public async Task Logout(int userId)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Id == userId)
                    .TagWith(nameof(Logout) + "_GetUser")
                    .Include(x => x.Tokens)
                    .FirstOrDefault();

            if (user == null)
                throw new CustomException(HttpStatusCode.BadRequest, "user", "User is not found");

            await _jwtService.ClearUserTokens(user);

            await _webSocketHandler.RemoveWebSocketConnectionByUserId(userId);
        }

        public async Task SendPasswordRestorationLink(string email, bool isAdmin = false)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Email == email)
              .Include(x => x.UserRoles)
                   .ThenInclude(x => x.Role)
               .FirstOrDefault();

            if (user == null
                || (!isAdmin && (!user.EmailConfirmed || !user.UserRoles.Any(x => x.Role.Name == Role.User)))
                || (isAdmin && !user.UserRoles.Any(x => x.Role.Name == Role.Admin || x.Role.Name == Role.SuperAdmin)))
                throw new CustomException(HttpStatusCode.BadRequest, "email", "Email not registered in the system");

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            var obj = new { Link = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/reset-password?email={HttpUtility.UrlEncode(user.Email)}&token={HttpUtility.UrlEncode(code)}" };

            try
            {
                await _emailService.SendAsync(new List<string>() { user.Email }, obj, EmailType.ResetPassword);
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "Email", ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task SendConfirmEmailLink(ApplicationUser user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var obj = new { Link = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/confirm-email?email={HttpUtility.UrlEncode(user.Email)}&token={HttpUtility.UrlEncode(code)}" };

            try
            {
                await _emailService.SendAsync(new List<string>() { user.Email }, obj, EmailType.ConfrimEmail);
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "Email", ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task<LoginResponseModel> ResetPassword(ResetPasswordRequestModel model, bool isAdmin = false)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Email == model.Email)
                .Include(x => x.VerificationTokens)
                .Include(x => x.Profile.Avatar)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefault();

            user.ThrowIfBlocked();

            if (user == null || isAdmin ? !user.UserRoles.Any(x => x.Role.Name == Role.Admin || x.Role.Name == Role.SuperAdmin) : !user.UserRoles.Any(x => x.Role.Name == Role.User))
                throw new CustomException(HttpStatusCode.BadRequest, "email", "Email is invalid");

            var token = HttpUtility.UrlDecode(model.Token).Replace(" ", "+");

            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

            if (!result.Succeeded)
                throw new CustomException(HttpStatusCode.BadRequest, "token", "Token is invalid");

            var loginResponse = await _jwtService.BuildLoginResponse(user);

            return loginResponse;
        }

        public async Task<LoginResponseModel> ResetPassword(ResetPasswordWithPhoneRequestModel model)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.PhoneNumber == model.PhoneNumber)
                .Include(x => x.Profile.Avatar)
                .Include(x => x.VerificationTokens)
                .FirstOrDefault();

            user.ThrowIfBlocked();

            if (user == null || !user.PhoneNumberConfirmed)
                throw new CustomException(HttpStatusCode.BadRequest, "phoneNumber", "Phone Number is invalid");

            var code = user.VerificationTokens.FirstOrDefault(x => !x.IsUsed && x.IsValid && x.TokenHash == _hashUtility.GetHash(model.Code));

            if (code == null)
                throw new CustomException(HttpStatusCode.BadRequest, "code", "Invalid code");

            await _userManager.ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user), model.Password);
            code.IsUsed = true;

            _unitOfWork.Repository<ApplicationUser>().Update(user);

            // there is _unitOfWork.SaveChanges call inside BuildLoginResponse
            var loginResponse = await _jwtService.BuildLoginResponse(user);

            return loginResponse;
        }

        public async Task ChangePassword(ChangePasswordRequestModel model, int userId)
        {
            if (model.OldPassword == model.Password)
                throw new CustomException(HttpStatusCode.BadRequest, "password", "New password matches old password");

            var user = await _userManager.FindByIdAsync(userId.ToString());

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.Password);

            if (!result.Succeeded)
                throw new CustomException(HttpStatusCode.BadRequest, "general", "Current password doesn’t match password");
        }

        public async Task SendPasswordRestorationCodeAsync(string phoneNumber)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.PhoneNumber == phoneNumber && x.PhoneNumberConfirmed)
                .Include(x => x.VerificationTokens)
                .FirstOrDefault();

            if (user == null)
                return;

            await _smsService.SendVerificationCodeAsync(user, user.PhoneNumber, VerificationCodeType.ResetPassword);
        }
    }
}
