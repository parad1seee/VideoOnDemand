using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Logging;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Services.Interfaces.External;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using Twilio.Types;
using Task = System.Threading.Tasks.Task;

namespace VideoOnDemand.Services.Services.External
{
    public class TwillioService : ITwillioService
    {
        private IConfiguration _configuration;
        private IUnitOfWork _unitOfWork;
        private ILogger<TwillioService> _logger;

        public TwillioService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<TwillioService> logger)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _logger = logger;

            TwilioClient.Init(_configuration["Twilio:AccountSid"], _configuration["Twilio:AuthToken"]);
        }

        public async Task SendMessageAsync(string to, string body)
        {
            string sender = _configuration["Twilio:Sender"];

            SMSLog log = new SMSLog
            {
                Sender = sender,
                Recipient = to,
                Text = body,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var message = await MessageResource.CreateAsync(
                    to: new PhoneNumber(to),
                    from: sender,
                    body: body);

                log.Status = SendingStatus.Success;
            }
            catch (ApiException e)
            {
                log.Status = SendingStatus.Failed;

                _logger.LogError($"Twilio Error {e.Code}({e.Message}) - {e.MoreInfo}");
                throw new SystemException(e.Message);
            }
            finally
            {
                _unitOfWork.Repository<SMSLog>().Insert(log);
                _unitOfWork.SaveChanges();
            }
        }

        public async Task<object> Call(string to)
        {
            var receiver = new PhoneNumber(to);
            var from = new PhoneNumber(_configuration["Twilio:Sender"]);

            var call = CallResource.Create(receiver, from, url: new Uri("https://jet-echidna-4440.twil.io/assets/CallVerefication.xml"));

            return call;
        }
    }
}
