using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Common.Utilities;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.InternalModels;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Socials;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.External
{
    public class GoogleService : SocialServiceBase, IGoogleService
    {
        private readonly HttpClient _httpClient;
        private readonly HashUtility _hashUtility;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJWTService _jwtService;
        private readonly IConfiguration _configuration = null;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISMSService _smsService;
        private readonly IImageService _imageService;

        public GoogleService(IConfiguration configuration, HttpClient httpClient, HashUtility hashUtility, UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork, ISMSService smsService, IJWTService jwtService, IImageService imageService)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _hashUtility = hashUtility;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _smsService = smsService;
            _jwtService = jwtService;
            _imageService = imageService;
        }

        public async Task<GProfileResponseModel> GetProfile(string token)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://openidconnect.googleapis.com/v1/userinfo?alt=json&access_token={token}");

                // Will throw an exception if not successful
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<GProfileResponseModel>(content);

                if (result == null)
                    throw new ArgumentException("Invalid token");

                return result;
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "token", "Google Token is invalid");
            }
        }

        public async Task<LoginResponseModel> ProcessRequest(GoogleWithPhoneRequestModel model)
        {
            var profile = await GetProfile(model.Token);

            var userWithGoogle = _unitOfWork.Repository<ApplicationUser>().Get(x => x.GoogleId == profile.Id)
                .Include(x => x.Profile.Avatar)
                .Include(x => x.VerificationTokens)
                .FirstOrDefault();

            // If there is such user in DB - just return 
            if (userWithGoogle != null)
            {
                BreakIfUserBlocked(userWithGoogle);
                var loginResponse = await _jwtService.BuildLoginResponse(userWithGoogle);

                return loginResponse;
            }
            else if (userWithGoogle == null && model.PhoneNumber != null)
            {
                // Check if there is such user in DB, if so - add to it google id
                var existingUser = await _unitOfWork.Repository<ApplicationUser>().Get(x => x.PhoneNumber == model.PhoneNumber)
                    .Include(x => x.Profile.Avatar)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    BreakIfUserBlocked(existingUser);
                    existingUser.GoogleId = profile.Id;

                    _unitOfWork.Repository<ApplicationUser>().Update(existingUser);
                    _unitOfWork.SaveChanges();

                    var loginResponse = await _jwtService.BuildLoginResponse(existingUser);

                    return loginResponse;
                }
                else
                {
                    // In other case create VerificationCode with user data and send core to user
                    try
                    {
                        var data = JsonConvert.SerializeObject(new RegisterWithSocialsUsingPhoneInternalModel
                        {
                            PhoneNumber = model.PhoneNumber,
                            SocialId = profile.Id
                        }, new JsonSerializerSettings { Formatting = Formatting.Indented });

                        await _smsService.SendVerificationCodeAsync(model.PhoneNumber, VerificationCodeType.ConfirmGoogle, data);
                    }
                    catch
                    {
                        throw new CustomException(HttpStatusCode.BadRequest, "phoneNumber", "Error while sending message");
                    }

                    throw new CustomException(HttpStatusCode.NoContent, "phoneNumber", "Verification code sent");
                }
            }
            else
            {
                throw new CustomException(HttpStatusCode.BadRequest, "token", "There is no user with such google id");
            }
        }

        public async Task<LoginResponseModel> ProcessRequest(GoogleWithEmailRequestModel model)
        {
            var profile = await GetProfile(model.Token);

            // If we didn`t get email from profile, get it from model
            var email = profile?.Email ?? model?.Email ?? null;

            var userWithGoogle = _unitOfWork.Repository<ApplicationUser>().Get(x => x.GoogleId == profile.Id)
                .Include(x => x.Profile.Avatar)
                .Include(x => x.VerificationTokens)
                .FirstOrDefault();

            // If there is such user in DB - just return 
            if (userWithGoogle != null)
            {
                BreakIfUserBlocked(userWithGoogle);
                var loginResponse = await _jwtService.BuildLoginResponse(userWithGoogle);

                return loginResponse;
            }
            else if (userWithGoogle == null && email != null)
            {
                // Check if there is such user in DB, if so - add to it google id
                var existingUser = await _unitOfWork.Repository<ApplicationUser>().Get(x => x.Email == email)
                    .Include(x => x.Profile.Avatar)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    BreakIfUserBlocked(existingUser);
                    existingUser.GoogleId = profile.Id;

                    _unitOfWork.Repository<ApplicationUser>().Update(existingUser);
                    _unitOfWork.SaveChanges();

                    var loginResponse = await _jwtService.BuildLoginResponse(existingUser);

                    return loginResponse;
                }
                else
                {
                    // In other case - create new user
                    var user = new ApplicationUser
                    {
                        Email = email,
                        UserName = email,
                        IsActive = true,
                        RegistratedAt = DateTime.UtcNow,
                        EmailConfirmed = false,
                        GoogleId = profile.Id
                    };

                    var result = await _userManager.CreateAsync(user);

                    if (!result.Succeeded)
                        throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

                    result = await _userManager.AddToRoleAsync(user, Role.User);

                    if (!result.Succeeded)
                        throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

                    //// upload images from google url and save to s3
                    //if (profile.Picture != null && profile.Picture.Any())
                    //{
                    //    // change last bool by your bucket privacy rules
                    //    var avatar = _imageService.DownloadImageFromUrl(profile.Picture, $"{DateTime.UtcNow.Ticks}google{user.Id}", true);

                    //    if (user.Profile == null)
                    //        user.Profile = new Profile();

                    //    user.Profile.AvatarId = avatar.Id;
                    //}

                    var loginResponse = await _jwtService.BuildLoginResponse(user);

                    return loginResponse;
                }
            }
            else
            {
                throw new CustomException(HttpStatusCode.BadRequest, "token", "There is no user with such google id");
            }
        }

        public async Task<LoginResponseModel> ConfrimRegistration(ConfirmPhoneRequestModel model)
        {
            var code = _unitOfWork.Repository<VerificationToken>()
                .Find(x => !x.IsUsed && x.IsValid && x.Type == VerificationCodeType.ConfirmGoogle && x.TokenHash == _hashUtility.GetHash(model.Code));

            if (code == null)
                throw new CustomException(HttpStatusCode.BadRequest, "code", "SMS code is not valid. Add correct code or re-send it");

            // Parse and create user
            var userData = JsonConvert.DeserializeObject<RegisterWithSocialsUsingPhoneInternalModel>(code.Data);

            var user = new ApplicationUser
            {
                PhoneNumber = userData.PhoneNumber,
                UserName = userData.PhoneNumber,
                IsActive = true,
                RegistratedAt = DateTime.UtcNow,
                PhoneNumberConfirmed = false,
                GoogleId = userData.SocialId
            };

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
                throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

            result = await _userManager.AddToRoleAsync(user, Role.User);

            if (!result.Succeeded)
                throw new CustomException(HttpStatusCode.BadRequest, "general", result.Errors.FirstOrDefault().Description);

            code.IsUsed = true;

            _unitOfWork.Repository<VerificationToken>().Update(code);
            _unitOfWork.SaveChanges();

            var loginResponse = await _jwtService.BuildLoginResponse(user);

            return loginResponse;
        }
    }
}
