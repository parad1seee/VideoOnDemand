using Mandrill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.Notifications;
using VideoOnDemand.Models.RequestModels.Test;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.Redis;
using VideoOnDemand.Redis.Models;
using VideoOnDemand.Redis.Store.Abstract;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.ScheduledTasks;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using VideoOnDemand.Services.Jobs;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    [Validate]
    public class TestController : _BaseApiController
    {
        private ILogger<TestController> _logger;
        private IUnitOfWork _unitOfWork;
        private ITwillioService _twillioService;
        private ISNSService _snsService;
        private IFCMService _fcmService;
        private IJWTService _jwtService;
        private IUserService _userService;
        private IRedisStore<RedisTestModel> _redisStore;
        private IMailChimpService _mailChimpService;
        private readonly RedisClient redisClient;
        private OneWeekAfterRegistration _oneWeekAfterRegistration;

        public TestController(IStringLocalizer<ErrorsResource> localizer,
            ILogger<TestController> logger,
            IUnitOfWork unitOfWork,
            ITwillioService twillioService,
            ISNSService snsService,
            IFCMService fcmService,
            IJWTService jwtService,
            IUserService userService,
            IServiceProvider serviceProvider,
            IMailChimpService mailChimpService,
            IRedisStore<RedisTestModel> redisStore,
            RedisClient redisClient)
            : base(localizer)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _twillioService = twillioService;
            _snsService = snsService;
            _fcmService = fcmService;
            _jwtService = jwtService;
            _userService = userService;
            _mailChimpService = mailChimpService;
            _redisStore = redisStore;
            this.redisClient = redisClient;
            _oneWeekAfterRegistration = serviceProvider.GetScheduledTask<OneWeekAfterRegistration>();
        }

        /// <summary>
        /// For Swagger UI
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizeWithoutCredentials([FromBody] ShortAuthorizationRequestModel model)
        {
            IQueryable<ApplicationUser> users = null;

            if (model.Id.HasValue)
                users = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Id == model.Id);
            else if (!string.IsNullOrEmpty(model.UserName))
                users = _unitOfWork.Repository<ApplicationUser>().Get(x => x.UserName == model.UserName);

            var user = await users.Include(x => x.Profile.Avatar).FirstOrDefaultAsync();

            if (user == null)
            {
                Errors.AddError("", "User is not found");
                return Errors.Error(HttpStatusCode.NotFound);
            }

            return Json(new JsonResponse<LoginResponseModel>(await _jwtService.BuildLoginResponse(user)));
        }

        // POST api/v1/test/sendSMS
        /// <summary>
        /// Send test SMS using Twillio. Only for dev purposes.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/test/SMS
        ///     {                
        ///         "phone" : "+447818425330",
        ///         "text" : "text"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 40X, 500 with message error</returns>
        [HttpPost("SendSMS")]
        [PreventSpam(Name = "SendSMS")]
        [AllowAnonymous]
        [SwaggerResponse(201, ResponseMessages.MessageSent, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> SendSms([FromBody] SendTestSMSRequestModel model)
        {
            await _twillioService.SendMessageAsync(model.Phone, model.Text);

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Sent")));
        }

        // POST api/v1/test/sms
        /// <summary>
        /// Send test SMS using AWS SNS. Only for dev purposes.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/test/sms
        ///     {                
        ///         "phone" : "+447818425330",
        ///         "text" : "text"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 40X, 500 with message error</returns>
        [HttpPost("sms")]
        [PreventSpam(Name = "sms")]
        [AllowAnonymous]
        [SwaggerResponse(200, ResponseMessages.MessageSent, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> SendSmsWithAWS([FromBody] SendTestSMSRequestModel model)
        {
            await _snsService.SendMessageAsync(model.Phone, model.Text);

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Sent")));
        }

        // POST api/v1/test/pushNotification
        /// <summary>
        /// Send Push notification to iOS or Android device
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/test/pushNotification
        ///
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 40X, 500 with message error</returns>
        [HttpPost("PushNotification")]
        [SwaggerResponse(201, ResponseMessages.MessageSent, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public async Task<IActionResult> SendPush(string deviceToken, string title, string body, [FromBody] PushNotificationData data)
        {

            if (deviceToken == null)
                return Errors.BadRequest("deviceToken", "Device Token is null");

            if (title == null || body == null)
                return Errors.BadRequest("title", "Title/Body is null");

            await _fcmService.SendPushNotification(deviceToken, new PushNotification(title, body, data), testMode: true);

            return Created(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Sent successfully")));
        }

        // DELETE api/v1/test/DeleteAccount
        /// <summary>
        /// Hard delete user from db
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE api/v1/test/DeleteAccount?userid=1
        ///
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 40X, 500 with message error</returns>
        [HttpDelete("DeleteAccount")]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        public IActionResult DeleteAccount([FromQuery] int userId)
        {
            if (userId <= 0)
                return Errors.BadRequest("userId", "Invalid user id");

            _userService.HardDeleteUser(userId);
            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("User has been deleted")));
        }

        // POST api/v1/test/restore-cron
        /// <summary>
        /// Restore cron
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/test/restore-cron
        ///
        /// </remarks>
        /// <returns>HTTP 200 with success message or HTTP 500 with message error</returns>
        [PreventSpam(Name = "RestoreCron")]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Restore-cron")]
        public async Task<IActionResult> RestoreCron()
        {
            var cancelTokenSource = new CancellationTokenSource();
            var token = cancelTokenSource.Token;

            await _oneWeekAfterRegistration.RestoreAsync(token);

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Cron restored")));
        }

        // POST api/v1/test/redis-test
        /// <summary>
        /// Resia test
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/test/redis-test
        ///
        /// </remarks>
        /// <returns>HTTP 200 with message or HTTP 500 with message error</returns>
        [AllowAnonymous]
        [PreventSpam(Name = "RedisTest")]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("redis-test")]
        public async Task<IActionResult> RedisTest()
        {
            // Set data
            var resitTestObject = new RedisTestModel
            {
                Id = 13 * DateTime.UtcNow.Millisecond,
                Text = "Test info",
                Date = DateTime.UtcNow.Date
            };

            await _redisStore.Set(resitTestObject.Id.ToString(), resitTestObject);

            //Get data
            var data = await _redisStore.Get(resitTestObject.Id.ToString());

            await redisClient.Subscribe<RedisTestModel>("test-channel", TestHandler);

            await redisClient.Send("test-channel", data);

            var keys = await _redisStore.GetByPattern("*");

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Ok")));
        }

        // POST api/v1/test/mailChimp-send-test
        /// <summary>
        /// Send test email using MailChimp. Only for dev purposes.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/test/mailChimp-send-test
        ///     {
        ///         "subject": "string",
        ///         "fromEmail": "example@gmail.com",
        ///         "fromName": "string",
        ///         "toEmails": [
        ///             "example@gmail.com"
        ///         ],
        ///         "templateName": "string"
        ///     }
        ///
        /// </remarks>
        [AllowAnonymous]
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<List<EmailResult>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("mailChimp-send-test")]
        public async Task<IActionResult> MandrillSendTest(SendMailChimpTestEmailRequestModel model)
        {
            var content = new Dictionary<string, string>();
            content.Add("*|CURRENT_YEAR|*", "2021");

            try
            {
                var result = await _mailChimpService.SendTransactionalEmail(model.Subject, model.FromEmail, model.FromName, model.ToEmails, model.TemplateName, content);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Errors.BadRequest("MailChimp", ex.InnerException?.Message ?? ex.Message);
            }
        }

        private async Task TestHandler(RedisTestModel model)
        {
            Debug.WriteLine("Subscribe");
        }
    }
}