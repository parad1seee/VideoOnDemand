using System.Threading.Tasks;
using VideoOnDemand.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using VideoOnDemand.Common.Extensions;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Logging;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Services.Interfaces.External;
using System.Collections.Generic;
using System.Linq;
using VideoOnDemand.Common.Exceptions;
using System.Net;

namespace VideoOnDemand.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private IUnitOfWork _unitOfWork;
        private IEmailTemplateService _template;
        private ISESService _sesService;

        private const string SupportEmail = "info@VideoOnDemand.com";

        public string CurrentDomain
        {
            get
            {
                var httpCtx = _httpContextAccessor.HttpContext;
                if (httpCtx == null)
                {
                    return null;
                }

                return $"{httpCtx.Request.Scheme}://{httpCtx.Request.Host}";
            }
        }

        public EmailService(IHttpContextAccessor contextAccessor, IWebHostEnvironment hostingEnvironment, IUnitOfWork unitOfWork, ISESService sesService)
        {
            _httpContextAccessor = contextAccessor;
            _hostingEnvironment = hostingEnvironment;
            _unitOfWork = unitOfWork;
            _sesService = sesService;
        }

        private async Task SendEmail(List<string> destinationEmails, object model, string template, string subject)
        {
            try
            {
                _template = new EmailTemplateService(_hostingEnvironment) { Template = template };
                _template.Layout = Path.Combine("wwwroot/HtmlContentTemplates/HtmlLayout.html");
                string html = _template.Render(model);

                string fromName = "VideoOnDemand app";

                EmailLog log = new EmailLog
                {
                    Sender = SupportEmail,
                    EmailBody = html,
                    CreatedAt = DateTime.UtcNow,
                    Status = SendingStatus.Failed
                };
                destinationEmails.ForEach(r => log.EmailRecepients.Add(new EmailRecipient() { Email = r }));

                try
                {
                    if (!destinationEmails.Any(e => e.Contains("@q.q")) && !destinationEmails.Any(e => e.Contains("@example.com")) && !destinationEmails.Any(e => e.Contains("@verified.com")))
                    {
                        var result = await _sesService.Send(subject: subject, from: SupportEmail, fromName: fromName, to: destinationEmails, bodyHtml: html);
                        log.Status = SendingStatus.Success;
                    }

                    _unitOfWork.Repository<EmailLog>().Insert(log);
                    _unitOfWork.SaveChanges();
                }
                catch (Exception ex)
                {
                    _unitOfWork.Repository<EmailLog>().Insert(log);
                    _unitOfWork.SaveChanges();
                    throw new Exception("Email sending failed", new Exception(" -> " + ex.Message));
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> SendAsync(List<string> destinationEmails, object model, EmailType emailType, string subject = null)
        {
            if (destinationEmails == null || !destinationEmails.Any())
                throw new ArgumentNullException();

            model.ThrowsWhenNull();
            switch (emailType)
            {
                case EmailType.SuccessfulRegistration:
                    await SendEmail(destinationEmails, model, Path.Combine("wwwroot/EmailTemplates/Registration.html"), subject != null ? subject : "Welcome");
                    break;
                case EmailType.ConfrimEmail:
                    await SendEmail(destinationEmails, model, Path.Combine("wwwroot/EmailTemplates/ConfirmEmail.html"), subject != null ? subject : "Confirm email");
                    break;
                case EmailType.ResetPassword:
                    await SendEmail(destinationEmails, model, Path.Combine("wwwroot/EmailTemplates/ResetPassword.html"), subject != null ? subject : "Change password");
                    break;
            }

            return true;
        }
    }
}