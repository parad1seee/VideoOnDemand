using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers.Attributes;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Socials;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Session;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    public class SocialsController : _BaseApiController
    {
        private UserManager<ApplicationUser> _userManager;
        private IJWTService _jwtService;
        private IGoogleService _googleService;
        private IFacebookService _facebookService;
        private ILinkedInService _linkedInService;
        private IUnitOfWork _unitOfWork;
        private ILogger<SocialsController> _logger;

        public SocialsController(IStringLocalizer<ErrorsResource> localizer, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork,
            IJWTService jwtService, IGoogleService googleService, IFacebookService facebookService, ILinkedInService linkedInService, ILogger<SocialsController> logger)
            : base(localizer)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _googleService = googleService;
            _facebookService = facebookService;
            _linkedInService = linkedInService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Register_Phone Google

        // POST api/v1/socials/sessions/phone/google
        /// <summary>
        /// Google login API
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/sessions/phone/google
        ///     {  
        ///         "phoneNumber" : "+447555557777",
        ///         "token" : "1111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 204, 400, 500 with error message</returns>  
        [AllowAnonymous]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(204, ResponseMessages.MessageSent, typeof(ErrorResponseModel))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Socials Phone" })]
        [HttpPost("Sessions/Phone/Google")]
        public async Task<IActionResult> Google([FromBody]GoogleWithPhoneRequestModel model)
        {
            var response = await _googleService.ProcessRequest(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        // PUT api/v1/socials/sessions/phone/google/confirm
        /// <summary>
        /// Confirm google registration
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/sessions/sessions/phone/google/confirm
        ///     {  
        ///         "phone" : "+44755555XXXX",
        ///         "code" : "1111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>  
        [AllowAnonymous]
        [PreventSpam(Name = "ConfirmGoogle")]
        [SwaggerResponse(201, ResponseMessages.SuccessfulRegistration, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Socials Phone" })]
        [HttpPut("Sessions/Phone/Google/Confirm")]
        public async Task<IActionResult> ConfirmGoogle([FromBody]ConfirmPhoneRequestModel model)
        {
            var response = await _googleService.ConfrimRegistration(model);
            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion

        #region Google with email

        // POST api/v1/socials/sessions/google
        /// <summary>
        /// Google login API
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/socials/sessions/google
        ///     {  
        ///         "email" : "test@test.com",
        ///         "token" : "1111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>  
        [AllowAnonymous]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Sessions/Google")]
        public async Task<IActionResult> Google([FromBody]GoogleWithEmailRequestModel model)
        {
            var response = await _googleService.ProcessRequest(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion

        #region LinkedIn with email 

        // POST  api/v1/socials/sessions/linkedin
        /// <summary>
        /// LinkedIn login API
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/socials/sessions/linkedin
        ///     {  
        ///         "email" : "test@test.com",
        ///         "token" : "1111",
        ///         "redirectUri" : "uri"
        ///     }
        ///
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>
        [AllowAnonymous]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Sessions/LinkedIn")]
        [Validate]
        public async Task<IActionResult> LinkedIn([FromBody]LinkedInWithEmailRequestModel model)
        {
            var response = await _linkedInService.ProcessRequest(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion

        #region Register_Phone LinkedIn

        // POST  api/v1/socials/sessions/phone/linkedin
        /// <summary>
        /// LinkedIn login API
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/socials/sessions/phone/linkedin
        ///     {  
        ///         "phone" : "+44755555XXXX",
        ///         "token" : "1111",
        ///         "redirectUri" : "uri"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 204, 400, 500 with error message</returns> 
        [AllowAnonymous]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(204, ResponseMessages.MessageSent, typeof(ErrorResponseModel))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Socials Phone" })]
        [HttpPost("Sessions/Phone/LinkedIn")]
        [Validate]
        public async Task<IActionResult> LinkedIn([FromBody]LinkedInWithPhoneRequestModel model)
        {
            var response = await _linkedInService.ProcessRequest(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        // PUT api/v1/socials/sessions/phone/linkedin/confirm
        /// <summary>
        /// Confirm linkedin registration
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/socials/sessions/phone/linkedin/confirm
        ///     {  
        ///         "phone" : "+44755555XXXX",
        ///         "code" : "1111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns> 
        [AllowAnonymous]
        [PreventSpam(Name = "ConfirmLinkedIn")]
        [SwaggerResponse(201, ResponseMessages.SuccessfulRegistration, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Socials Phone" })]
        [HttpPut("Sessions/Phone/LinkedIn/Confirm")]
        public async Task<IActionResult> ConfirmLinkedIn([FromBody]ConfirmPhoneRequestModel model)
        {
            var response = await _linkedInService.ConfrimRegistration(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion

        #region Facebook with email 

        // POST api/v1/socials/sessions/facebook
        /// <summary>
        /// Facebook login API
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/socials/sessions/facebook
        ///     {  
        ///         "email" : "example@example.com",
        ///         "token" : "1111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 400, 500 with error message</returns>  
        [AllowAnonymous]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [HttpPost("Sessions/Facebook")]
        public async Task<IActionResult> Facebook([FromBody]FacebookWithEmailRequestModel model)
        {
            var response = await _facebookService.ProcessRequest(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion

        #region Register_Phone Facebook

        // POST api/v1/socials/sessions/phone/facebook
        /// <summary>
        /// Facebook login API
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/socials/sessions/phone/facebook
        ///     {  
        ///         "phoneNumber" : "+447555557777",
        ///         "token" : "1111"
        ///     }
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 204, 400, 500 with error message</returns>
        [AllowAnonymous]
        [SwaggerResponse(201, ResponseMessages.SuccessfulLogin, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(204, ResponseMessages.MessageSent, typeof(ErrorResponseModel))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Socials Phone" })]
        [HttpPost("Sessions/Phone/Facebook")]
        public async Task<IActionResult> Facebook([FromBody]FacebookWithPhoneRequestModel model)
        {
            var response = await _facebookService.ProcessRequest(model);

            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        // PUT api/v1/socials/sessions/phone/facebook/confirm
        /// <summary>
        /// Confirm facebook registration
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT api/v1/socials/sessions/phone/facebook/confirm
        ///     {  
        ///         "phone" : "+44755555XXXX",
        ///         "code" : "1111"
        ///     }
        ///
        /// </remarks>
        /// <returns>HTTP 201 with login response or HTTP 204, 400, 500 with error message</returns>  
        [AllowAnonymous]
        [PreventSpam(Name = "ConfirmFacebook")]
        [SwaggerResponse(201, ResponseMessages.SuccessfulRegistration, typeof(JsonResponse<LoginResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidCredentials, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [SwaggerOperation(Tags = new[] { "Socials Phone" })]
        [HttpPut("Sessions/Phone/Facebook/Confirm")]
        public async Task<IActionResult> ConfirmFacebook([FromBody]ConfirmPhoneRequestModel model)
        {
            var response = await _facebookService.ConfirmFacebookRegistration(model);
            return Created(new JsonResponse<LoginResponseModel>(response));
        }

        #endregion
    }
}