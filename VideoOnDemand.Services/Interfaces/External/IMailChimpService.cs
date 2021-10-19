using Mandrill.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface IMailChimpService
    {
        /// <summary>
        /// Send transactional email using MailChimp
        /// </summary>
        /// <param name="subject">Email subject</param>
        /// <param name="fromEmail">Sender`s email address</param>
        /// <param name="fromName">Senser`s name</param>
        /// <param name="to">List of email addresses of recipients</param>
        /// <param name="templateName">Email template name</param>
        /// <param name="templateContent">Data to render in template</param>
        /// <returns></returns>
        Task<List<EmailResult>> SendTransactionalEmail(string subject, string fromEmail, string fromName, List<string> to, string templateName, Dictionary<string, string> templateContent);
    }
}
