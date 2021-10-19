using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Base.CursorPagination;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Base.CursorPagination;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.Exporting;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API.Admin
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/admin-users")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Role.SuperAdmin + "," + Role.Admin)]
    [Validate]
    public class AdminUsersController : _BaseApiController
    {
        private UserManager<ApplicationUser> _userManager;
        private IUserService _userService;
        private IExportService _exportService;
        private ILogger<AdminUsersController> _logger;

        public AdminUsersController(IStringLocalizer<ErrorsResource> localizer,
            UserManager<ApplicationUser> userManager,
            IUserService userService,
            IExportService exportService,
            ILogger<AdminUsersController> logger)
             : base(localizer)
        {
            _userManager = userManager;
            _exportService = exportService;
            _userService = userService;
            _logger = logger;
        }

        // GET api/v1/admin-users
        /// <summary>
        /// Get user profile
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/v1/admin-users/3    
        /// 
        /// </remarks>
        /// <param name="id">Id of user</param>
        /// <returns>HTTP 200 with user profile or 40X, 500 with error message</returns>
        [HttpGet("{id}")]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> GetProfile([FromRoute]int id)
        {
            if (id <= 0)
                return Errors.BadRequest("Id", "Invalid Id");

            var data = await _userService.GetProfileAsync(id);

            return Json(new JsonResponse<UserResponseModel>(data));

        }

        // POST api/v1/admin-users
        /// <summary>
        /// Change user password
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/admin/users
        ///     {                
        ///        "id": 0,
        ///        "password": "111111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 and confirmation message, or HTTP 40X, 500 with errors</returns> 
        [HttpPost]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> ChangePassword([FromBody]ChangeUserPasswordRequestModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id.ToString());

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, code, model.Password);

            if (result.Succeeded)
            {
                return Created(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Password has been changed")));
            }
            else
            {
                Errors.AddError("general", "Can`t change password");
                return Errors.InternalServerError();
            }
        }

        // GET api/v1/admin-users
        /// <summary>
        /// Retrieve users in pagination
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET api/v1/admin-users?Search=xsdfadsf&amp;Order.Key=Id&amp;Order.Direction=Asc&amp;Limit=45&amp;Offset=45
        /// 
        /// </remarks>
        /// <param name="model">Pagination request model</param>
        /// <returns>HTTP 200 with users list in pagination or 40X, 500 with error message</returns>  
        [HttpGet]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonPaginationResponse<List<UserTableRowResponseModel>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public IActionResult GetAll([FromQuery]PaginationRequestModel<UserTableColumn> model)
        {
            var data = _userService.GetAll(model);

            return Json(new JsonPaginationResponse<List<UserTableRowResponseModel>>(data.Data, model.Offset + model.Limit, data.TotalCount));
        }

        // GET api/v1/admin-users/cursor
        /// <summary>
        /// Retrieve users in cursor pagination
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET api/v1/admin-users/cursor?Search=xsdfadsf&amp;Order.Key=Id&amp;Order.Direction=Asc&amp;Limit=45&amp;LastId=10
        /// 
        /// </remarks>
        /// <param name="model">Cursor pagination request model</param>
        /// <returns>HTTP 200 with users list in pagination or 40X, 500 with error message</returns>  
        [HttpGet("cursor")]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(CursorJsonPaginationResponse<List<UserTableRowResponseModel>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public IActionResult GetAllByCursor([FromQuery] CursorPaginationRequestModel<UserTableColumn> model)
        {
            var data = _userService.GetAll(model);

            return Json(new CursorJsonPaginationResponse<List<UserTableRowResponseModel>>(data.Data, data.LastId));
        }

        // PATCH api/v1/admin-users/{id}
        /// <summary>
        /// Block/unblock user
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PATCH /api/v1/admin-users/2
        /// 
        /// </remarks>
        /// <param name="id">Id of user</param>
        /// <returns>HTTP 200 with user profile or 40X, 500 with error message</returns> 
        [HttpPatch("{id}")]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> SwitchUserState([FromRoute]int id)
        {
            if (User.IsInRole(Role.User))
                return Forbidden();

            if (id <= 0)
                return Errors.BadRequest("Id", "Invalid Id");

            var data = await _userService.SwitchUserActiveState(id);

            return Json(new JsonResponse<UserResponseModel>(data));
        }

        // DELETE api/v1/admin-users/{id}
        /// <summary>
        /// Delete user
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     DELETE /api/v1/admin-users/2
        /// 
        /// </remarks>
        /// <param name="id">Id of user</param>
        /// <returns>HTTP 200 with user profile or 40X, 500 with error message</returns>   
        [HttpDelete("{id}")]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public IActionResult Delete([FromRoute]int id)
        {
            if (id <= 0)
                return Errors.BadRequest("Id", "Invalid Id");

            var data = _userService.SoftDeleteUser(id);

            return Json(new JsonResponse<UserResponseModel>(data));
        }

        // GET api/v1/admin-users/pdf
        /// <summary>
        /// Get pdf document with list of users
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET api/v1/admin-users/pdf?Key=Id&amp;Direction=Asc
        ///     
        /// </remarks>
        /// <param name="order">Ordering request model</param>
        /// <returns>HTTP 200 with Pdf file or 40X, 500 with error message</returns> 
        [HttpGet("pdf")]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(IFormFile))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> GetUsersPdfTable([FromQuery]OrderingRequestModel<UserTableColumn, SortingDirection> order)
        {
            var response = await _exportService.ExportUsersTable(ExportFormat.Pdf, order);
            return File(response, "application/pdf", "UsersList.pdf");
        }

        // GET api/v1/admin-users/xls
        /// <summary>
        /// Get xls document with list of users
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET api/v1/admin-users/xls?Key=Id&amp;Direction=Asc
        ///     
        /// </remarks>
        /// <param name="order">Ordering request model</param>
        /// <returns>HTTP 200 with Xls file or 40X, 500 with error message</returns> 
        [HttpGet("xls")]
        [SwaggerOperation(Tags = new[] { "Admin Users" })]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(IFormFile))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> GetUsersXlsTable([FromQuery]OrderingRequestModel<UserTableColumn, SortingDirection> order)
        {
            var response = await _exportService.ExportUsersTable(ExportFormat.Xls, order);
            return File(response, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UsersList.xls");
        }
    }
}