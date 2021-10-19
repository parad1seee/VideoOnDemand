using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme/*, Roles = Role.User*/)]
    public class UploadController : _BaseApiController
    {
        private IImageService _imageService;
        private IHttpContextAccessor _httpContextAccessor;
        private ILogger<UploadController> _logger;

        public UploadController(IStringLocalizer<ErrorsResource> localizer,
            IImageService imageService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UploadController> logger)
             : base(localizer)
        {
            _httpContextAccessor = httpContextAccessor;
            _imageService = imageService;
            _logger = logger;
        }

        // POST api/v1/upload/image
        /// <summary>
        /// Upload Image
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/upload/image
        ///     
        /// </remarks>
        /// <returns>HTTP 201 with image model or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<ImageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [DisableRequestSizeLimit]
        [HttpPost("Image")]
        public async Task<IActionResult> Image(IFormFile file, ImageType imageType, bool isS3LinkOpen)
        {
            if (file == null)
                return Errors.BadRequest("Image", "Failed image uploading");

            var response = await _imageService.UploadOne(file, imageType, isS3LinkOpen);

            return Created(new JsonResponse<ImageResponseModel>(response));
        }

        // POST api/v1/upload/video
        /// <summary>
        /// Upload Video
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/upload/video
        ///     
        /// </remarks>
        /// <returns>HTTP 201 with image model or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<ImageResponseModel>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [DisableRequestSizeLimit]
        [HttpPost("Video")]
        public async Task<IActionResult> Video(IFormFile file)
        {
            if (file == null)
                return Errors.BadRequest("Image", "Failed video uploading");

            await _imageService.UploadVideo(file);

            return Created(new JsonResponse<MessageResponseModel>(new("Sok")));
        }

        // POST api/v1/upload/multipleimages
        /// <summary>
        /// Upload multiple Images
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/v1/upload/multipleimages
        ///     
        /// </remarks>
        /// <returns>HTTP 201 with image model list or HTTP 40X, 500 with error message</returns>
        [SwaggerResponse(201, ResponseMessages.RequestSuccessful, typeof(JsonResponse<List<ImageResponseModel>>))]
        [SwaggerResponse(400, ResponseMessages.InvalidData, typeof(ErrorResponseModel))]
        [SwaggerResponse(401, ResponseMessages.Unauthorized, typeof(ErrorResponseModel))]
        [SwaggerResponse(403, ResponseMessages.Forbidden, typeof(ErrorResponseModel))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [DisableRequestSizeLimit]
        [HttpPost("MultipleImages")]
        public async Task<IActionResult> MultipleImages(List<IFormFile> images, ImageType imageType, bool isS3LinkOpen)
        {
            if (!images.Any())
                return Errors.BadRequest("Image", "Failed image uploading");

            var response = await _imageService.UploadMultiple(images, imageType, isS3LinkOpen);

            return Created(new JsonResponse<List<ImageResponseModel>>(response));
        }
    }
}