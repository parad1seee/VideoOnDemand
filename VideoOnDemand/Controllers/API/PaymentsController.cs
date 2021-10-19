using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.Payments;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Payments;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces.External;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Role.User)]
    [Validate]
    public class PaymentsController : _BaseApiController
    {
        private IConfiguration _configuration;
        private IBraintreeService _braintreeService;
        private IStripeService _stripeService;
        private ILogger<PaymentsController> _logger;

        public PaymentsController(IConfiguration configuration, IStringLocalizer<ErrorsResource> errorsLocalizer, IBraintreeService braintreeService,
            IStripeService stripeService, ILogger<PaymentsController> logger)
            : base(errorsLocalizer)
        {
            _configuration = configuration;
            _braintreeService = braintreeService;
            _stripeService = stripeService;
            _logger = logger;
        }

        #region Braintree

        // GET api/v1/Payments/Braintree/ClientToken
        /// <summary>
        /// Generate a Braintree client token that initialize client-side Braintree SDK
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/Payment/Braintree/ClientToken
        ///
        /// </remarks>
        /// <returns>HTTP 200 with Braintree client token or errors with an HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<SingleTokenResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet("Braintree/ClientToken")]
        public async Task<IActionResult> GetBraintreeClientToken()
        {
            return Json(new JsonResponse<SingleTokenResponseModel>(new SingleTokenResponseModel { Token = await _braintreeService.GetClientTokenAsync(User.GetUserId()) }));
        }

        // POST api/v1/Payments/BraintreeTestPayment
        /// <summary>
        /// API for Braintree test payment
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/Payments/BraintreeTestPayment
        ///     {
        ///         "amount": "10",
        ///         "nonce": "example_payment_nonce"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Payment nounce and amount</param>
        /// <returns>HTTP 201 with id of the transaction or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.SuccessfulPayment, typeof(JsonResponse<StripeIdResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Braintree/TestPayment")]
        public async Task<IActionResult> MakeBraintreeTestPayment([FromBody]BraintreePaymentRequestModel model)
        {
            var successPaymentResult = await _braintreeService.MakePaymentAsync(model.Nonce, model.Amount);

            //IdResponseModel changed to StripeIdResponseModel - stripe id is string, not int
            return Created(new JsonResponse<StripeIdResponseModel>(new StripeIdResponseModel { Id = successPaymentResult.Target.Id }));
        }

        // GET api/v1/Payments/Braintree/PaymentMethods
        /// <summary>
        /// Get payment methods associated with user that has specified id
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/Payments/Braintree/PaymentMethods?userId=123
        ///
        /// </remarks>
        /// <param name="userId">Id of the user</param>
        /// <returns>HTTP 200 with list of user's payment methods HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<PaymentMethodsResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet("Braintree/PaymentMethods")]
        public async Task<IActionResult> GetPaymentMethods([Required]int userId)
        {
            var customer = await _braintreeService.FindCustomerByIdAsync(userId);

            return Json(new JsonResponse<PaymentMethodsResponseModel>(new PaymentMethodsResponseModel { PaymentMethods = customer.PaymentMethods.Select(i => i.Token).ToList() }));
        }

        #endregion

        #region Stripe

        // POST api/v1/Payments/Stripe/TestPayment
        /// <summary>
        /// API for Stripe test payment
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/Payments/Stripe/TestPayment
        ///     {
        ///         "userId": "123",
        ///         "cardToken": "tok_visa",
        ///         "amount": "100",
        ///         "currency": "usd"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Information about payment, card and user that initiate payment</param>
        /// <returns>HTTP 201 with id of created transaction or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.SuccessfulPayment, typeof(JsonResponse<StripeIdResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Stripe/TestPayment")]
        public async Task<IActionResult> MakeStripeTestPayment([FromBody]StripePaymentRequestModel model)
        {
            //IdResponseModel changed to StripeIdResponseModel - stripe id is string, not int
            return Created(new JsonResponse<StripeIdResponseModel>(await _stripeService.MakePaymentAsync(User.GetUserId(), model.CardToken, model.Amount, model.Currency)));
        }

        // POST api/v1/Payments/Stripe/Cards
        /// <summary>
        /// Add new card to cards list associated with Stripe customer
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/Payments/Stripe/Cards
        ///     {
        ///         "userId": "123",
        ///         "cardToken": "tok_visa"
        ///     }
        ///
        /// </remarks>
        /// <param name="model">Braintree card token</param>
        /// <returns>HTTP 201 with id of added card or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<StripeIdResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Stripe/Cards")]
        public async Task<IActionResult> AddCard([FromBody]TokenRequestModel model)
        {
            //IdResponseModel changed to StripeIdResponseModel - stripe id is string, not int
            return Created(new JsonResponse<StripeIdResponseModel>(await _stripeService.AddCardAsync(User.GetUserId(), model.Token)));
        }

        // DELETE api/v1/Payments/Stripe/Cards
        /// <summary>
        /// Remove card from cards list associated with Stripe customer
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE api/v1/Payments/Stripe/Cards?cardToken=tok_visa
        ///
        /// </remarks>
        /// <param name="cardId">Braintree card id</param>
        /// <returns>HTTP 200 with success message or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<MessageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpDelete("Stripe/Cards")]
        public async Task<IActionResult> RemoveCard([Required]string cardId)
        {
            await _stripeService.RemoveCardAsync(User.GetUserId(), cardId);

            return Json(new JsonResponse<MessageResponseModel>(new MessageResponseModel("Card successfully removed")));
        }

        // GET api/v1/Payments/Stripe/Cards
        /// <summary>
        /// Get list of all cards associated with Stripe customer
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/Payments/Stripe/Cards
        ///
        /// </remarks>
        /// <returns>HTTP 200 with list of customer's cards or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(JsonResponse<List<StripeIdResponseModel>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpGet("Stripe/Cards")]
        public async Task<IActionResult> GetAllCards()
        {
            await _stripeService.GetAllCardsAsync(User.GetUserId());

            return Json(new JsonResponse<List<StripeIdResponseModel>>(await _stripeService.GetAllCardsAsync(User.GetUserId())));
        }

        [AllowAnonymous]
        [HttpPost("Stripe/Webhook")]
        public async Task<IActionResult> ProcessStripeWebhook()
        {
            await _stripeService.ProcessWebhook(HttpContext, Request);

            return Json(new JsonResponse<IdResponseModel>(new IdResponseModel { }));
        }

        #endregion
    }
}