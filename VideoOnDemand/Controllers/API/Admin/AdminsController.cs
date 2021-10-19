using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API.Admin
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/superadmin/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Role.SuperAdmin)]
    public class AdminsController : _BaseApiController
    {
        private IUserService _userService;
        private ILogger<AdminUsersController> _logger;

        public AdminsController(IStringLocalizer<ErrorsResource> localizer, IUserService userService, ILogger<AdminUsersController> logger)
             : base(localizer)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET api/v1/superadmin/admins
        /// <summary>
        /// Retrieve administrators in pagination
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     
        ///     GET api/v1/superadmin/admins?Search=string&amp;Order.Key=Id&amp;Limit=10&amp;Offset=10
        ///     
        /// </remarks>
        /// <param name="model">Pagination request model</param>
        /// <returns>An administrators list in pagination</returns>
        [HttpGet]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonPaginationResponse<List<UserTableRowResponseModel>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public IActionResult GetAdministrators([FromQuery]PaginationRequestModel<UserTableColumn> model)
        {
            if (!User.IsInRole(Role.SuperAdmin))
                return Forbidden();

            var data = _userService.GetAll(model, true);

            return Json(new JsonPaginationResponse<List<UserTableRowResponseModel>>(data.Data, model.Offset + model.Limit, data.TotalCount));

        }

        // PATCH api/v1/superadmin/admins/{id}
        /// <summary>
        /// Block/unblock admins
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PATCH api/v1/superadmin/admins/2        
        /// 
        /// </remarks>
        /// <param name="id">Id of admin</param>
        /// <returns>An admin profile</returns>  
        [HttpPatch("{id}")]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> SwitchAdminState([FromRoute]int id)
        {
            if (!User.IsInRole(Role.SuperAdmin))
                return Forbidden();

            if (id <= 0)
            {
                Errors.AddError("Id", "Invalid Id");
                return Errors.BadRequest();
            }

            var data = await _userService.SwitchUserActiveState(id);

            return Json(new JsonResponse<UserResponseModel>(data));
        }
    }
}