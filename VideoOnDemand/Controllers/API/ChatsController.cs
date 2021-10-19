using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Chat;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Chat;
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
    public class ChatsController : _BaseApiController
    {
        private IChatService _chatService;
        private ILogger<ChatsController> _logger;

        public ChatsController(IStringLocalizer<ErrorsResource> localizer, IChatService chatService, ILogger<ChatsController> logger)
            : base(localizer)
        {
            _chatService = chatService;
            _logger = logger;
        }

        #region Chats

        // POST api/v1/chats
        /// <summary>
        /// Create chat
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/chats
        ///     {                
        ///         "chatOpponentsIds": [1, 2]
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with new chat model or 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<ChatBaseResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody]CreateChatRequestModel model)
        {
            if (model.ChatOpponentsIds.Count < 1)
                return Errors.BadRequest("ChatOpponentsIds", "Invalid count of chat opponents");

            var chat = await _chatService.CreateChat(model);
            return Created(new JsonResponse<ChatBaseResponseModel>(chat));
        }

        // GET api/v1/chats
        /// <summary>
        /// Get chat list
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/chats?limit=20&amp;offset=0
        ///     
        /// </remarks>
        /// <param name="model">Chat list request model</param>
        /// <returns>HTTP 200 with chat list or 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonPaginationResponse<List<ChatBaseResponseModel>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet]
        public async Task<IActionResult> GetChatList([FromQuery]PaginationBaseRequestModel model)
        {
            var data = await _chatService.GetChatList(model);

            return Json(new JsonPaginationResponse<List<ChatResponseModel>>(data.Data, model.Offset + model.Limit, data.TotalCount));
        }

        // GET api/v1/chats/{id}
        /// <summary>
        /// Get chat
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/chats/1
        ///
        /// </remarks>
        /// <param name="id">Chat id</param>
        /// <returns>HTTP 200 and chat with info about last sended message or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<ChatBaseResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChat([FromRoute]int id)
        {
            if (id < 1)
                return Errors.BadRequest("id", "Id must be greater than zero");

            var data = await _chatService.GetChat(id);
            return Json(new JsonResponse<ChatBaseResponseModel>(data));
        }

        // GET api/v1/chats/{id}/status
        /// <summary>
        /// Get user status
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/chats/1/status
        ///
        /// </remarks>
        /// <param name="id">User id</param>
        /// <returns>HTTP 200 and chat with info about last sended message or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UserStatusResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatus([FromRoute]int id)
        {
            if (id < 1)
                return Errors.BadRequest("id", "Id must be greater than zero");

            var data = await _chatService.GetUserStatus(id);
            return Json(new JsonResponse<UserStatusResponseModel>(data));
        }

        #endregion

        #region Messages

        // POST api/v1/chats/{id}/messages
        /// <summary>
        /// Send new message
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/chats/1/messages
        ///     {
        ///         "text": "msg text",
        ///         "imageId": "path"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with New Message or 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<ChatMessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("{chatid}/messages")]
        public async Task<IActionResult> SendMessage([FromRoute]int chatid, [FromBody]ChatMessageRequestModel model)
        {
            if (chatid < 1)
                return Errors.BadRequest("chatid", "Chat Id must be greater than zero");

            var message = await _chatService.SendMessage(chatid, model);

            return Created(new JsonResponse<ChatMessageResponseModel>(message));
        }

        // GET api/v1/chats/{id}/messages
        /// <summary>
        /// Get message list
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/chats/1/messages?limit=20&amp;offset=0
        ///     
        /// </remarks>
        /// <param name="chatid">Id of Chat</param>
        /// <param name="model">Message list request model</param>
        /// <returns>HTTP 200 with message list or 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonPaginationResponse<List<ChatMessageBaseResponseModel>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet("{chatid}/messages")]
        public async Task<IActionResult> GetMessages([FromRoute]int chatid, [FromQuery] ChatMessagesListRequestModel model)
        {
            if (chatid < 1)
                return Errors.BadRequest("chatid", "Chat Id must be greater than zero");

            var data = await _chatService.GetMessages(chatid, model);

            return Json(new JsonPaginationResponse<List<ChatMessageBaseResponseModel>>(data.Data, model.Offset + model.Limit, data.TotalCount));
        }

        // PATCH api/v1/chats/{id}/messages
        /// <summary>
        /// Read messages in chat
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PATCH api/v1/chats/1/messages?MessageIds=1&amp;MessageIds=2&amp;ReadTillMessageId=1
        ///
        /// </remarks>
        /// <returns>HTTP 200 with total count of unread messages in Chat or 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<UnreadMessagesResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPatch("{chatid}/messages")]
        public async Task<IActionResult> ReadMessages([FromRoute]int chatid, [FromQuery]ReadMessagesRequestModel model)
        {
            if (chatid < 1)
                return Errors.BadRequest("chatid", "Chat Id must be greater than zero");

            var response = await _chatService.ReadMessages(chatid, model);

            return Json(new JsonResponse<UnreadMessagesResponseModel>(response));
        }

        #endregion
    }
}