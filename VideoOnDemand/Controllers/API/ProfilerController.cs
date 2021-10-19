using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VideoOnDemand.ResourceLibrary;
using StackExchange.Profiling;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VideoOnDemand.Controllers.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    public class ProfilerController : _BaseApiController
    {
        public ProfilerController(IStringLocalizer<ErrorsResource> localizer)
            :base (localizer)
        {
        }

        // GET: api/v1/profiler
        // url to see last profile check: http://localhost:xxxxx/profiler/results
        // profile available in swagger page too
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string firstUrl = string.Empty;
            string secondUrl = string.Empty;
            using (MiniProfiler.Current.Step("Get method"))
            {
                using (MiniProfiler.Current.Step("Prepare data"))
                {
                    using (MiniProfiler.Current.CustomTiming("SQL", "SELECT * FROM Config"))
                    {
                        // Simulate a SQL call
                        Thread.Sleep(500);
                        firstUrl = "https://google.com";
                        secondUrl = "https://stackoverflow.com/";
                    }
                }
                using (MiniProfiler.Current.Step("Use data for http call"))
                {
                    using (MiniProfiler.Current.CustomTiming("HTTP", "GET " + firstUrl))
                    {
                        var client = new HttpClient();
                        var reply = await client.GetAsync(firstUrl);
                    }

                    using (MiniProfiler.Current.CustomTiming("HTTP", "GET " + secondUrl))
                    {
                        var client = new HttpClient();
                        var reply = await client.GetAsync(secondUrl);
                    }
                }
            }
            return Ok();
        }
    }
}
