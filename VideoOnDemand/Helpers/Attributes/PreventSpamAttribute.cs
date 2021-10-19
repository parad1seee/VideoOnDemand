using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.ResourceLibrary;
using System;
using System.Net;

namespace VideoOnDemand.Helpers.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PreventSpamAttribute : ActionFilterAttribute
    {
        public string Name { get; set; }
        public int Seconds { get; set; } = 3;
        public string Message { get; set; }

        private static MemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

        public override void OnActionExecuting(ActionExecutingContext c)
        {
            IStringLocalizer<ErrorsResource> errorsLocalizer = c.HttpContext.RequestServices.GetService<IStringLocalizer<ErrorsResource>>();
            var key = string.Concat(Name, "-", c.HttpContext.Request.HttpContext.Connection.RemoteIpAddress);

            if (!Cache.TryGetValue(key, out bool entry))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(Seconds));

                Cache.Set(key, true, cacheEntryOptions);
            }
            else
            {
                if (string.IsNullOrEmpty(Message))
                    Message = "You may only perform this action every {n} seconds.";

                var errorResponse = new ErrorResponseModel(errorsLocalizer) { Code = ErrorCode.Conflict };
                errorResponse.AddError("general", Message.Replace("{n}", Seconds.ToString()));

                c.Result = new ContentResult()
                {
                    Content = JsonConvert.SerializeObject(errorResponse, new JsonSerializerSettings { Formatting = Formatting.Indented }),
                    StatusCode = (int)HttpStatusCode.Conflict,
                    ContentType = "application/json"
                };
            }
        }
    }
}
