using Mandrill;
using Mandrill.Models;
using Mandrill.Requests.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Logging;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Services.Interfaces.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.External
{
    public class MailChimpService : IMailChimpService
    {
        private IConfiguration _configuration;
        private ILogger<MailChimpService> _logger;
        private IUnitOfWork _unitOfWork;

        public MailChimpService(IConfiguration configuration, ILogger<MailChimpService> logger, IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<EmailResult>> SendTransactionalEmail(string subject, string fromEmail, string fromName, List<string> to, string templateName, Dictionary<string, string> templateContent)
        {
            using (var client = new MandrillApi(_configuration["MailChimp:MandrillKey"]))
            {
                var message = new EmailMessage
                {
                    FromEmail = fromEmail,
                    FromName = fromName,
                    Subject = subject,
                    To = to.Select(t => new EmailAddress(t)).ToList()
                };

                List<TemplateContent> templateContents = new List<TemplateContent>();

                foreach (var content in templateContent)
                    templateContents.Add(new TemplateContent { Name = content.Key, Content = content.Value });

                var request = new SendMessageTemplateRequest(message, templateName, templateContents);

                try
                {
                    var results = await client.SendMessageTemplate(request);

                    foreach (var result in results)
                    {
                        EmailLog log = new EmailLog { CreatedAt = DateTime.UtcNow, Sender = fromEmail };
                        log.EmailRecepients.Add(new EmailRecipient { Email = result.Email });

                        if (result.Status == EmailResultStatus.Rejected || result.Status == EmailResultStatus.Invalid)
                            log.Status = SendingStatus.Failed;
                        else
                            log.Status = SendingStatus.Success;
                        
                        _unitOfWork.Repository<EmailLog>().Insert(log);
                    }
                    _unitOfWork.SaveChanges();

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError("MailChimp Error: " + ex.InnerException?.Message ?? ex.Message);
                    throw;
                }
            }
        }
    }
}
