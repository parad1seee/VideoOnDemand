using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Validate]
    public class UsersController : _BaseApiController
    {
        private IUserService _userService;
        private IAccountService _accountService;

        public UsersController(IStringLocalizer<ErrorsResource> localizer, IUserService userService, IAccountService accountService)
             : base(localizer)
        {
            _userService = userService;
            _accountService = accountService;
        }

        // POST api/v1/users
        /// <summary>
        /// Register new user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/users
        ///     {                
        ///         "email" : "test@example.com",
        ///         "password" : "1simplepassword",
        ///         "confirmPassword" : "1simplepassword"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with user email and info about email status or HTTP 4XX, 500 with error message</returns>
        [AllowAnonymous]
        [PreventSpam(Name = "Register")]
        [SwaggerResponse(201, ResponseMessages.SuccessfulRegistration, typeof(JsonResponse<RegisterResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(422, ResponseMessages.EmailAlreadyRegistered, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost]
        [Validate]
        public async Task<IActionResult> Register([FromBody]RegisterRequestModel model)
        {
            var response = await _accountService.Register(model);
            
            return Created(new JsonResponse<RegisterResponseModel>(response));
        }

        #region Register_Phone

        // POST api/v1/users/phone
        /// <summary>
        /// Register new user using phone
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/users/phone
        ///     {       
        ///         "phone" : "+44755555XXXX",
        ///         "password" : "1simplepassword",
        ///         "confirmPassword" : "1simplepassword",
        ///         "deviceToken": "device1"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with user data or HTTP 4XX, 500 with error message</returns>
        [AllowAnonymous]
        [PreventSpam(Name = "Register")]
        [SwaggerResponse(201, ResponseMessages.SuccessfulRegistration, typeof(JsonResponse<RegisterUsingPhoneResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(422, ResponseMessages.EmailAlreadyRegistered, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Users Phone" })]
        [HttpPost("Phone")]
        public async Task<IActionResult> Register([FromBody]RegisterUsingPhoneRequestModel model)
        {
            var response = await _accountService.RegisterUsingPhone(model);

            return Created(new JsonResponse<RegisterUsingPhoneResponseModel>(response));
        }

        #endregion

        // POST api/v1/users/me/devices
        /// <summary>
        /// Add new Device
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/users/me/devices
        ///     {                
        ///        "deviceToken": "token"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and message if device added, or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.LinkSent, typeof(JsonResponse<UserDeviceResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Me/Devices")]
        public async Task<IActionResult> SetDeviceToken([FromBody]DeviceTokenRequestModel model)
        {
            var response = await _userService.SetDeviceToken(model, User.GetUserId());

            return Created(new JsonResponse<UserDeviceResponseModel>(response));
        }

        // PUT api/v1/users/me/password
        /// <summary>
        /// Change user password
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/users/me/password
        ///     {                
        ///        "oldPassword": "qwerty",
        ///        "password": "111111",
        ///        "confirmPassword": "111111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and confirmation message, or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPut("Me/Password")]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordRequestModel model)
        {
            await _accountService.ChangePassword(model, User.GetUserId());

            return Created(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Password has been changed")));
        }

        #region Profile

        // GET api/v1/users/me/profile
        /// <summary>
        /// Get my profile
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/users/me/profile
        ///
        /// </remarks>
        /// <returns>HTTP 201 with user profile or 401, 500 with error message</returns>   
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserResponseModel>))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet("Me/Profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var data = await _userService.GetProfileAsync(User.GetUserId());

            return Created(new JsonResponse<UserResponseModel>(data));
        }

        // PATCH api/v1/users/me/profile
        /// <summary>
        /// Edit profile
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PATCH api/v1/users/me/profile
        ///     {                
        ///        "firstName": "FirstName",
        ///        "lastName": "LastName",
        ///        "imageId": 1
        ///     }
        ///
        /// </remarks>
        /// <param name="model">User profile</param>
        /// <returns>HTTP 200 with user profile or 40X, 500 with error message</returns> 
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPatch("Me/Profile")]
        public async Task<IActionResult> EditMyProfile([FromBody]UserProfileRequestModel model)
        {
            var data = await _userService.EditProfileAsync(User.GetUserId(), model);

            return Json(new JsonResponse<UserResponseModel>(data));
        }

        // DELETE api/v1/users/me/profile/avatar
        /// <summary>
        /// Delete current avatar
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE api/v1/users/me/profile/avatar
        ///
        /// </remarks>
        /// <returns>HTTP 200 with user profile or 401, 500 with error message</returns>  
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserResponseModel>))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpDelete("Me/Profile/Avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            var data = _userService.DeleteAvatar(User.GetUserId());

            return Json(new JsonResponse<UserResponseModel>(data));
        }

        #endregion
    }
}