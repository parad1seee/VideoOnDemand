using System;
using System.IO;
using System.Reflection;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Common.Extensions;
using Microsoft.AspNetCore.Hosting;

namespace VideoOnDemand.Services.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public EmailTemplateService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public string Template { get; set; }

        public string Layout { get; set; }

        public string Render(object model)
        {
            model.ThrowsWhenNull(nameof(model));

            if (string.IsNullOrWhiteSpace(Template))
                throw new InvalidOperationException("Set the Template property");

            if (string.IsNullOrWhiteSpace(Layout))
                throw new InvalidOperationException("Set the Layout property");

            Template = File.ReadAllText(Template);
            Layout = File.ReadAllText(Layout);

            Layout = Layout.Replace($"[%CONTENT%]", Template);

            PropertyInfo[] props = model.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (prop.CanRead)
                {
                    string key = $"[%{prop.Name.ToUpper()}%]";
                    Layout = Layout.Replace(key, prop.GetValue(model)?.ToString());
                }
            }

            return this.Layout;
        }
    }
}