using Castle.Core.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using VideoOnDemand.Controllers.API;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Helpers;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.ResourceLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Wangkanai.Detection.Models;
using Wangkanai.Detection.Services;

namespace VideoOnDemand.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : _BaseApiController
    {
        private IStringLocalizer<ErrorsResource> _localizer;
        private UserManager<ApplicationUser> _userManager;
        private IDeviceService _device;

        public HomeController(UserManager<ApplicationUser> userManager, IDetectionService detectionService, IStringLocalizer<ErrorsResource> localizer)
             : base(localizer)
        {
            _userManager = userManager;
            _device = detectionService.Device;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            return Redirect("swagger");
        }

        [HttpGet("WebSocketInfo")]
        public IActionResult WebSocketInfo()
        {
            return View();
        }

        [ProducesResponseType(typeof(ErrorResponseModel), 400)]
        [ProducesResponseType(typeof(ErrorResponseModel), 500)]
        [HttpGet("reset-password")]
        public async Task<IActionResult> ResetPassword(string email, string token)
        {
            try
            {
                email = HttpUtility.UrlDecode(email).Replace(" ", "+");

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return Errors.NotFound("user", "User is not found");

                var isAdmin = await _userManager.IsInRoleAsync(user, Role.Admin);

                // TODO : CHANGE LINKS
                if (_device.Type == Device.Mobile && !isAdmin)
                    return Redirect($"TEMPLATENAME://reset_password?email={email}&token={token}");
                else if (isAdmin)
                    return Redirect($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/web-admin/resetpassword?email={email}&token={token}");
                else
                    return Redirect($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/resetpassword?email={email}&token={token}");
            }
            catch (Exception ex)
            {
                Errors.AddError("general", ex.InnerException?.Message ?? ex.Message);
                return Errors.InternalServerError(ex.StackTrace);
            }
        }

        [ProducesResponseType(typeof(ErrorResponseModel), 400)]
        [ProducesResponseType(typeof(ErrorResponseModel), 500)]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            try
            {
                //throw new Exception();
                email = HttpUtility.UrlDecode(email).Replace(" ", "+");

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return Errors.NotFound("user", "User is not found");

                // TODO : CHANGE LINKS
                if (_device.Type == Device.Mobile)
                    return Redirect($"TEMPLATENAME://confirm_email?email={email}&token={token}");
                else
                    return Redirect($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/confirm_email?email={email}&token={token}");
            }
            catch (Exception ex)
            {
                Errors.AddError("general", ex.InnerException?.Message ?? ex.Message);
                return Errors.InternalServerError(ex.StackTrace);
            }
        }

        [HttpGet("ShowLogsDirectory")]
        public IActionResult ShowLogsDirectory(string dir)
        {
            var path = "Logs/" + dir;
            Dictionary<string, string> directories = new Dictionary<string, string>();
            Dictionary<string, string> files = new Dictionary<string, string>();

            if (Directory.Exists(path))
            {
                directories = Directory.GetDirectories(path).Select(t => new KeyValuePair<string, string>(t.Substring(5), "")).ToDictionary(k => k.Key, v => v.Value);
                files = Directory.GetFiles(path).Select(t => new KeyValuePair<string, string>(t.Substring(5), ConvertFileSize(t))).ToDictionary(k => k.Key, v => v.Value);
            }

            if (!dir.IsNullOrEmpty())
            {
                path = path.TrimEnd('/', '\\');

                var directoryName = Path.GetFileName(path);
                var prev = path.Substring(0, path.Length - directoryName.Length).Substring(5);
                ViewBag.Prev = prev;
            }
            else
                ViewBag.Prev = null;

            return View(new List<Dictionary<string, string>> { directories, files });
        }

        [HttpGet("ShowLog")]
        public IActionResult ShowLog(string path)
        {
            var text = System.IO.File.ReadAllText(Path.Combine("Logs/", path));

            text = string.Join("\r\n", text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Reverse());

            return Content(text, "application/json");
        }

        [HttpGet("ClearLogs")]
        public IActionResult ClearLogs()
        {
            DirectoryInfo di = new DirectoryInfo("Logs");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            return Content("OK");
        }

        // Handle api errors 
        [Route("ApiError/{id}")]
        public IActionResult ApiError(int? id = null)
        {
            if (!id.HasValue)
                id = Response.StatusCode;

            return StatusCodeHanler(id.Value);
        }

        // Handle ui page errors 
        [Route("Error")]
        public IActionResult Error()
        {
            int statusCode = Response.StatusCode;

            // TODO: change status code handler logic for ui pages
            return StatusCodeHanler(statusCode);
        }

        // Use this method to return json with error data
        private IActionResult StatusCodeHanler(int statusCode)
        {
            var errorResponse = ErrorHelper.GetError(_localizer, statusCode);

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(errorResponse, new JsonSerializerSettings { Formatting = Formatting.Indented }),
                StatusCode = statusCode,
                ContentType = "application/json"
            };
        }

        private string ConvertFileSize(string filename)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = new FileInfo(filename).Length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        #region Test Views

        [HttpGet("BraintreeCardTest")]
        public IActionResult BraintreeCardTest()
        {
            return View();
        }

        [HttpGet("BraintreePayPalTest")]
        public IActionResult BraintreePayPalTest()
        {
            return View();
        }

        #endregion
    }
}