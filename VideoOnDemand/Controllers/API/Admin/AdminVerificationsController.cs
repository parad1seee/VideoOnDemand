using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API.Admin
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/admin-verifications")]
    [Validate]
    public class AdminVerificationsController : _BaseApiController
    {
        private IAccountService _accountService;

        public AdminVerificationsController(IStringLocalizer<ErrorsResource> errorsLocalizer, IAccountService accountService)
            : base(errorsLocalizer)
        {
            _accountService = accountService;
        }

        // POST api/v1/admin-verifications/password
        /// <summary>
        /// Forgot password - Send code to change password via email
        /// </summary>
        /// <remarks>
        /// 
        /// Sample request:
        ///
        ///     POST api/v1/admin-verifications/password
        ///     {
        ///         "email": "email@example.com"
        ///     }
        /// 
        /// </remarks>
        /// <returns>HTTP 201 with success message or HTTP 400, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Admin Verifications" })]
        [HttpPost("password")]
        public async Task<IActionResult> SendPasswordRestorationLink([FromBody]EmailRequestModel model)
        {
            await _accountService.SendPasswordRestorationLink(model.Email, true);

            return Created(new JsonResponse<MessageResponseModel>(new MessageResponseModel("If we found this email address in our database we have sent you password reset instructions by email")));
        }

        // PUT api/v1/admin-verifications/password
        /// <summary>
        /// ResetPassword - Change admin's password
        /// </summary>
        /// <remarks>
        /// 
        /// Sample request:
        ///
        ///     PUT api/v1/admin-verifications/password
        ///     {
        ///         "token": "token_example",
        ///         "password": "stringG1",
        ///         "confirmPassword": "stringG1"
        ///     }
        /// 
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Admin Verifications" })]
        [HttpPut("password")]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordRequestModel model)
        {
            var response = await _accountService.ResetPassword(model, true);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }
    }
}