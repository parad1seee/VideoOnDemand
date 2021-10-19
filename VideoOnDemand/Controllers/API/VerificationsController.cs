using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using System.Web;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Validate]
    public class VerificationsController : _BaseApiController
    {
        private UserManager<ApplicationUser> _userManager;
        private IAccountService _accountService;
        private ISMSService _sMSService;
        private IConfiguration _configuration = null;
        private ICallService _callService;

        public VerificationsController(IStringLocalizer<ErrorsResource> localizer, UserManager<ApplicationUser> userManager, IAccountService accountService, ISMSService sMSService, IConfiguration configuration, ICallService callService)
              : base(localizer)
        {
            _userManager = userManager;
            _accountService = accountService;
            _sMSService = sMSService;
            _configuration = configuration;
            _callService = callService;
        }

        // PUT api/v1/verifications/email
        /// <summary>
        /// Confirm user email
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/verifications/email
        ///     {     
        ///         "email" : "test@email.com",
        ///         "token": "some token"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and login response, or HTTP 400, 500 with error message</returns>
        [AllowAnonymous]
        [PreventSpam(Name = "ConfirmEmail")]
        [ProducesResponseType(typeof(JsonResponse<LoginResponseModel>), 200)]
        [SwaggerResponse(201, ResponseMessages.LinkSent, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPut("Email")]
        public async Task<IActionResult> ConfirmEmail([FromBody]ConfirmEmailRequestModel model)
        {
            var response = await _accountService.ConfirmEmail(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        // POST api/v1/verifications/password
        /// <summary>
        /// Forgot password - Send link to change password by email
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/verifications/password
        ///     {                
        ///        "email": "test@email.com"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and message if link sended, or HTTP 400, 500 with error message</returns> 
        [AllowAnonymous]
        [PreventSpam(Name = "ForgotPassword")]
        [SwaggerResponse(201, ResponseMessages.LinkSent, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.EmailInvalidOrNotConfirmed, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Password")]
        public async Task<IActionResult> ForgotPassword([FromBody]EmailRequestModel model)
        {
            await _accountService.SendPasswordRestorationLink(model.Email);

            return Created(new JsonResponse<MessageResponseModel>(new MessageResponseModel("If we found this email address in our database we have sent you password reset instructions by email")));
        }

        // POST api/v1/verifications/token
        /// <summary>
        /// Forgot password - Check if token is invalid or expired
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/verifications/token
        ///     {     
        ///         "email" : "test@email.com",
        ///         "token": "some token"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and message if token is checked, or HTTP 400, 500 with error message</returns>
        [AllowAnonymous]
        [PreventSpam(Name = "CheckResetPasswordToken")]
        [SwaggerResponse(201, ResponseMessages.LinkSent, typeof(JsonResponse<CheckResetPasswordTokenResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Token")]
        public async Task<IActionResult> CheckResetPasswordToken([FromBody]CheckResetPasswordTokenRequestModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            var token = HttpUtility.UrlDecode(model.Token).Replace(" ", "+");

            return Created(new JsonResponse<CheckResetPasswordTokenResponseModel>(new CheckResetPasswordTokenResponseModel
            {
                IsValid = await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token)
            }));
        }

        // PUT api/v1/verifications/password
        /// <summary>
        /// Forgot password - Change user password
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/verifications/password
        ///     {     
        ///        "email" : "test@email.com",
        ///        "token": "some token",
        ///        "password" : "1simplepassword",
        ///        "confirmPassword" : "1simplepassword" 
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and login response, or HTTP 400, 500 with error message</returns>  
        [AllowAnonymous]
        [PreventSpam(Name = "ResetPassword")]
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.EmailInvalidOrNotConfirmed, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.AccountBlocked, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPut("Password")]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordRequestModel model)
        {
            var response = await _accountService.ResetPassword(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #region Register_Phone

        // POST api/v1/verifications/phone/password
        /// <summary>
        /// Send SMS with confirmation code to specified phone number so that user can restore password
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/verifications/phone/password
        ///     {
        ///         "phoneNumber": "+44755555XXXX"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with success message or HTTP 40X, 500 with error message</returns>
        [AllowAnonymous]
        [PreventSpam(Name = "ForgotPassword")]
        [SwaggerResponse(201, ResponseMessages.MessageSent, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [Validate]
        [SwaggerOperation(Tags = new[] { "Verifications Phone" })]
        [HttpPost("Phone/Password")]
        public async Task<IActionResult> ForgotPassword([FromBody]PhoneNumberRequestModel model)
        {
            await _accountService.SendPasswordRestorationCodeAsync(model.PhoneNumber);

            return Created(new JsonResponse<MessageResponseModel>(new MessageResponseModel("If we found this phone number in our database we have sent you password reset code")));
        }

        // PUT api/v1/verifications/phone
        /// <summary>
        /// Confirm user phone number and finish registration
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/verifications/phone
        ///     {  
        ///         "phoneNumber" : "+44755555XXXX",
        ///         "code" : "1111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>  
        [AllowAnonymous]
        [PreventSpam(Name = "ConfirmPhone")]
        [SwaggerResponse(201, ResponseMessages.SuccessfulRegistration, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Verifications Phone" })]
        [HttpPut("Phone")]
        public async Task<IActionResult> ConfirmPhone([FromBody]ConfirmPhoneRequestModel model)
        {
            var response = await _accountService.ConfirmPhone(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        // PUT api/v1/verifications/phone/password
        /// <summary>
        /// Forgot password - Change user password
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/verifications/phone/password
        ///     {     
        ///        "phoneNumber" : "test@email.com",
        ///        "token": "some token",
        ///        "password" : "1simplepassword",
        ///        "confirmPassword" : "1simplepassword" 
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and message if link sended, or HTTP 400, 500 with error message</returns>
        [AllowAnonymous]
        [PreventSpam(Name = "ResetPassword")]
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.EmailInvalidOrNotConfirmed, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Verifications Phone" })]
        [HttpPut("Phone/Password")]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordWithPhoneRequestModel model)
        {
            var response = await _accountService.ResetPassword(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion

        // GET api/v1/verifications/phone/code
        /// <summary>
        /// Send phone verification code 
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET api/v1/verifications/phone/code
        /// 
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 500 with error message</returns>
        [PreventSpam(Name = "TwilioSendCode")]
        [SwaggerResponse(200, ResponseMessages.MessageSent, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Verifications Phone" })]
        [HttpGet("Phone/Code")]
        public async Task<IActionResult> TwilioSendCode()
        {
            var user = await _userManager.FindByIdAsync(User.GetUserId().ToString());
            await _sMSService.SendVerificationCodeAsync(user, user.PhoneNumber, Models.Enums.VerificationCodeType.Confirm);

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Code sent")));
        }

        // GET api/v1/verifications/phone/makeCall
        /// <summary>
        /// Make call
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET api/v1/verifications/phone/makeCall
        /// 
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 500 with error message</returns>
        [PreventSpam(Name = "MakeCall")]
        [SwaggerResponse(200, ResponseMessages.MessageSent, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Verifications Phone" })]
        [HttpGet("Phone/MakeCall")]
        public async Task<IActionResult> MakeCall()
        {
            await _callService.VerificationCall(await _userManager.FindByIdAsync(User.GetUserId().ToString()));

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Done")));
        }
    }
}