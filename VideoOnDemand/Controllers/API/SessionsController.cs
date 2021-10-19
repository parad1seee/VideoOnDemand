using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Role.User)]
    [Validate]
    public class SessionsController : _BaseApiController
    {
        private IAccountService _accountService;

        public SessionsController(IStringLocalizer<ErrorsResource> localizer, IAccountService accountService)
              : base(localizer)
        {
            _accountService = accountService;
        }

        // POST api/v1/sessions
        /// <summary>
        /// Login User. 'accessTokenLifetime' - access token life time (sec)
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/sessions
        ///     {                
        ///         "email" : "test@email.com",
        ///         "password" : "1simplepassword",
        ///         "accessTokenLifetime": "60" 
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>  
        [AllowAnonymous]
        [PreventSpam(Name = "Login", Seconds = 1)]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.AccountBlocked, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody]LoginRequestModel model)
        {
            var response = await _accountService.Login(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #region Register_Phone

        // POST api/v1/sessions/phone
        /// <summary>
        /// Login User. 'accessTokenLifetime' - access token life time (sec)
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/sessions/phone
        ///     {                
        ///         "phone" : "+380777777777",
        ///         "password" : "1simplepassword",
        ///         "accessTokenLifetime": "60" 
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>  
        [AllowAnonymous]
        [PreventSpam(Name = "Login", Seconds = 1)]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(405, ResponseMessages.AccountBlocked, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Phone")]
        public async Task<IActionResult> Login([FromBody]LoginWithPhoneRequestModel model)
        {
            var response = await _accountService.LoginUsingPhone(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion

        // DELETE api/v1/sessions
        /// <summary>
        /// Clear user tokens
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE api/v1/sessions
        ///
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 400, 500 with error message</returns>
        /// <response code="200">Logout successful</response>
        /// <response code="401">Unauthorized</response>   
        /// <response code="404">If the user is not found</response>  
        /// <response code="500">Internal server error</response>  
        [HttpDelete]
        [PreventSpam(Name = "Logout")]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(404, ResponseMessages.NotFound, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> Logout()
        {
            await _accountService.Logout(User.GetUserId());

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("You have been logged out")));
        }

        // PUT api/v1/sessions
        /// <summary>
        /// Refresh user access token
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/sessions
        ///     {                
        ///         "refreshToken" : "some token"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with new access-refresh token pair or HTTP 40X, 500 with error message</returns> 
        [AllowAnonymous]
        [PreventSpam(Name = "RefreshToken")]
        [RefreshTokenRoleValidation(new[] { Role.User })]
        [ProducesResponseType(typeof(JsonResponse<TokenResponseModel>), 200)]
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<TokenResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPut]
        public async Task<IActionResult> RefreshToken([FromBody]RefreshTokenRequestModel model)
        {
            var response = await _accountService.RefreshTokenAsync(model.RefreshToken, new List<string> { Role.User });

            return Created(new JsonResponse<TokenResponseModel>(response));
        }
    }
}