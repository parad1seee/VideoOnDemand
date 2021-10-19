using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API.Admin
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/admin-settings")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Role.SuperAdmin + "," + Role.Admin)]
    public class AdminSettingsController : _BaseApiController
    {
        private IAccountService _accountService;
        private ILogger<AdminSettingsController> _logger;

        public AdminSettingsController(IStringLocalizer<ErrorsResource> errorsLocalizer, IAccountService accountService, ILogger<AdminSettingsController> logger)
            : base(errorsLocalizer)
        {
            _accountService = accountService;
            _logger = logger;
        }

        // PUT api/v1/admin-settings/password
        /// <summary>
        /// Change admin's password
        /// </summary>
        /// <remarks>
        /// 
        /// Sample request:
        ///
        ///     PUT api/v1/admin-settings/password
        ///     {
        ///         "oldPassword": "stringG1",
        ///         "password": "stringG2",
        ///         "confirmPassword": "stringG2"
        ///     }
        /// 
        /// </remarks>
        /// <returns>HTTP 201 with success message or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Admin Settings" })]
        [Validate]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordRequestModel model)
        {
            await _accountService.ChangePassword(model, User.GetUserId());

            return Created(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Password has been changed")));
        }
    }
}