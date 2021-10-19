using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Utilities;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Domain.Entities.Logging;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace VideoOnDemand.Services.Services.External
{
    public class SmsService : ISMSService
    {
        private IConfiguration _configuration;
        private IUnitOfWork _unitOfWork;
        private HashUtility _hashService;
        private ILogger<SmsService> _logger;

        public SmsService(IConfiguration configuration, IUnitOfWork unitOfWork, HashUtility hashService, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _hashService = hashService;
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
                _unitOfWork.Repository<SMSLog>().Insert(log);
                _unitOfWork.SaveChanges();
            }
            catch (ApiException e)
            {
                log.Status = SendingStatus.Failed;
                _unitOfWork.Repository<SMSLog>().Insert(log);
                _unitOfWork.SaveChanges();

                _logger.LogError($"Twilio Error {e.Code}({e.Message}) - {e.MoreInfo}");
                throw new SystemException(e.Message);
            }
        }

        public async Task<bool> SendVerificationCodeAsync(string phoneNumber, VerificationCodeType type, string data = null)
        {
            string code = GenerateCode();

            bool isTestMode = phoneNumber.StartsWith("+4475555");

            var verificationToken = new VerificationToken
            {
                CreateDate = DateTime.UtcNow,
                TokenHash = _hashService.GetHash(isTestMode ? "1111" : code),
                PhoneNumber = phoneNumber,
                Type = type,
                Data = data
            };

            _unitOfWork.Repository<VerificationToken>().Insert(verificationToken);
            _unitOfWork.SaveChanges();

            if (!isTestMode)
                await SendMessageAsync(phoneNumber, $"Your verification code: {code}");

            _logger.LogError($"Your verification code: {code}");
            return true;
        }

        public async Task<bool> SendVerificationCodeAsync(ApplicationUser user, string phoneNumber, VerificationCodeType type, string data = null)
        {
            string code = GenerateCode();

            bool isTestMode = phoneNumber.StartsWith("+4475555");

            user.VerificationTokens.ToList().ForEach(x => _unitOfWork.Repository<VerificationToken>().Delete(x));

            user.VerificationTokens.Add(new VerificationToken
            {
                CreateDate = DateTime.UtcNow,
                TokenHash = _hashService.GetHash(isTestMode ? "1111" : code),
                PhoneNumber = phoneNumber,
                Type = type,
                Data = data
            });

            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();

            if (!isTestMode)
                await SendMessageAsync(phoneNumber, $"Your verification code: {code}");

            _logger.LogError($"Your verification code: {code}");
            return true;
        }

        private string GenerateCode()
        {
            int min = 1000;
            int max = 9999;
            Random rdm = new Random();
            return rdm.Next(min, max).ToString();
        }

    }
}
