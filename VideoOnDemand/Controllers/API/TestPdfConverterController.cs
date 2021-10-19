using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.PdfGenerator.Interfaces;
using VideoOnDemand.ResourceLibrary;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    public class TestPdfConverterController : _BaseApiController
    {
        private IStringLocalizer<ErrorsResource> _localizer = null;
        private ILogger<TestPdfConverterController> _logger = null;
        private IHtmlToPdfConverter _htmlToPdfConverter = null;

        public TestPdfConverterController(IStringLocalizer<ErrorsResource> localizer, ILogger<TestPdfConverterController> logger, IHtmlToPdfConverter htmlToPdfConverter)
          : base(localizer)
        {
            _localizer = localizer;
            _logger = logger;
            _htmlToPdfConverter = htmlToPdfConverter;
        }

        // GET api/v1/TestPdfConverterController/Test
        /// <summary>
        /// Returns test file
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET api/v1/TestPdfConverterController/Test
        ///     
        /// </remarks>
        /// <returns>HTTP 200 with test file or HTTP 500 with error message</returns>
        /// <response code="200">Test file</response>
        /// <response code="500">Internal server Error</response>   
        [SwaggerResponse(200, ResponseMessages.RequestSuccessful, typeof(IFormFile))]
        [SwaggerResponse(500, ResponseMessages.InternalServerError, typeof(ErrorResponseModel))]
        [DisableRequestSizeLimit]
        [HttpGet("Test")]
        public async Task<IActionResult> DownloadTestFile()
        {
            var response = _htmlToPdfConverter.ConvertHtmlToPdf("<h1>Test</h1><h2>Test</h2><h3>Test</h3><h4>Test</h4>");

            return File(response, "application/pdf", "test.pdf");
        }
    }
}