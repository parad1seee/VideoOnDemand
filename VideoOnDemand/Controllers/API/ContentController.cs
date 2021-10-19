using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.ResourceLibrary;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ContentController : _BaseApiController
    {
        private readonly IS3Service _s3Service;
        private readonly IImageService _imageService;

        private const int cacheAgeSeconds = 60 * 60 * 24 * 30; // 30 days

        public ContentController(IStringLocalizer<ErrorsResource> errorsLocalizer, IS3Service s3Service, IImageService imageService)
             : base(errorsLocalizer)
        {
            _s3Service = s3Service;
            _imageService = imageService;
        }

        // GET api/v1/content/{file}
        /// <summary>
        /// Returns file by name
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/content/asdf.png
        ///     
        /// </remarks>
        /// <returns>HTTP 200, or errors with an HTTP 500</returns>
        /// <response code="200">File</response>
        /// <response code="400">If the params are invalid</response>  
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal server Error</response> 
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [ProducesResponseType(typeof(ErrorResponseModel), 400)]
        [ProducesResponseType(typeof(ErrorResponseModel), 401)]
        [ProducesResponseType(typeof(ErrorResponseModel), 403)]
        [ProducesResponseType(typeof(ErrorResponseModel), 500)]
        [HttpGet("{file}")]
        public async Task<IActionResult> GetFromBucket([FromRoute] string file)
        {
            var stream = await _s3Service.GetFile(file);
            var extention = file.Substring(file.LastIndexOf('.') + 1);

            string content = $"image/png";
            if (_imageService.Extensions.Contains(extention))
                content = $"image/{extention}";

            Response.Headers["Cache-Control"] = $"public,max-age={cacheAgeSeconds}";

            return File(stream, content, file, true);
        }
    }
}
