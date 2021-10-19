using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.Payments;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces.External;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Validate]
    public class SubscriptionsController : _BaseApiController
    {
        private IBraintreeService _braintreeService;
        private IStripeService _stripeService;
        private ILogger<SubscriptionsController> _logger;

        public SubscriptionsController(IStringLocalizer<ErrorsResource> localizer,
            IBraintreeService braintreeService,
            IStripeService stripeService,
            ILogger<SubscriptionsController> logger)
            : base(localizer)
        {
            _braintreeService = braintreeService;
            _stripeService = stripeService;
            _logger = logger;
        }

        #region Braintree

        // POST api/v1/Subscriptions/Braintree
        /// <summary>
        /// Create Braintree subscription by associating payment method with specified plan
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/Subscriptions/Braintree
        ///     {
        ///         "paymentMethodToken": "jd68slm",
        ///         "planId": "example_plan_id"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Token of the payment method and id of the plan</param>
        /// <returns>HTTP 201 with id of the created subscription or HTTP 40X, 500 with error message</returns>
        [ProducesResponseType(typeof(JsonResponse<IdResponseModel>), 200)]
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<StripeIdResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Braintree")]
        public async Task<IActionResult> CreateBraintreeSubscription([FromBody]BraintreeSubscriptionRequestModel model)
        {
            var successPaymentResult = await _braintreeService.CreateSubscriptionAsync(model.PaymentMethodToken, model.PlanId);

            //IdResponseModel changed to StripeIdResponseModel - stripe id is string, not int
            return Created(new JsonResponse<StripeIdResponseModel>(new StripeIdResponseModel { Id = successPaymentResult.Target.Id }));
        }

        // POST api/v1/Subscriptions/Braintree/Webhook
        /// <summary>
        /// Endpoint to receive and process Braintree's subscriptions webhooks
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/Subscriptions/Braintree/Webhook
        ///     {
        ///         "bt_signature": "example_bt_signature",
        ///         "bt_payload": "example_bt_payload"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Braintree signature and payload</param>
        /// <returns>HTTP 201 with received notification kind or HTTP 400, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<string>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [AllowAnonymous]
        [HttpPost("Braintree/Webhook")]
        public async Task<IActionResult> TestBraintreeSubscriptionWebhook([FromForm]BraintreeSubscriptionWebhookRequestModel model)
        {
            var webhookResult = _braintreeService.ProcessSubscriptionWebhook(model.bt_signature, model.bt_payload);

            System.Diagnostics.Trace.WriteLine($"Braintree/Webhook -> ({DateTime.Now.ToString("G")}) {webhookResult}");

            return Created(new JsonResponse<string>(webhookResult));
        }

        #endregion

        #region Stripe

        // POST api/v1/Subscriptions/Stripe
        /// <summary>
        /// Create Stripe subscription with specified plan
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/Subscriptions/Stripe
        ///     {
        ///         "planId": "example_plan_id"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Plan id</param>
        /// <returns>HTTP 201 with id of the created subscription or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<StripeIdResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Stripe")]
        [Validate]
        public async Task<IActionResult> CreateStripeSubscription([FromBody]StripeCreateSubscriptionRequestModel model)
        {
            var result = await _stripeService.CreateSubscription(model.PlanId, User.GetUserId());

            //IdResponseModel changed to StripeIdResponseModel - stripe id is string, not int
            return Created(new JsonResponse<StripeIdResponseModel>(result));
        }

        // DELETE api/v1/Subscriptions/Stripe/
        /// <summary>
        /// Cancel Stripe subscription
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE api/v1/Subscriptions/Stripe
        ///     {
        ///         "subscriptionId" : "someId"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Subscription id</param>
        /// <returns>HTTP 201 with id of the canceled subscription or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<StripeIdResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpDelete("Stripe")]
        [Validate]
        public async Task<IActionResult> CancelStripeSubscription([FromBody]StripeSubscriptionRequestModel model)
        {
            var result = await _stripeService.CancelSubscription(model.SubscriptionId, User.GetUserId());

            //IdResponseModel changed to StripeIdResponseModel - stripe id is string, not int
            return Created(new JsonResponse<StripeIdResponseModel>(result));
        }

        #endregion
    }
}