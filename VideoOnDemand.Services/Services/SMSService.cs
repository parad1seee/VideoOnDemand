using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Utilities;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using System;
using System.Linq;
using System.Threading.Tasks;
using Twilio;

namespace VideoOnDemand.Services.Services
{
    public class SMSService : ISMSService
    {
        private IConfiguration _configuration;
        private IUnitOfWork _unitOfWork;
        private ITwillioService _twillioService;
        private HashUtility _hashService;
        private ILogger<SMSService> _logger;

        public SMSService(IConfiguration configuration, IUnitOfWork unitOfWork, ITwillioService twillioService, HashUtility hashService, ILogger<SMSService> logger)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _twillioService = twillioService;
            _hashService = hashService;
            _logger = logger;

            TwilioClient.Init(_configuration["Twilio:AccountSid"], _configuration["Twilio:AuthToken"]);
        }
        
        public async Task<bool> SendVerificationCodeAsync(string phoneNumber, VerificationCodeType type, string data = null)
        {
            string code = CodeGenerator.GenerateCode();

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
                await _twillioService.SendMessageAsync(phoneNumber, $"Your verification code: {code}");

            _logger.LogError($"Your verification code: {code}");
            return true;
        }

        public async Task<bool> SendVerificationCodeAsync(ApplicationUser user, string phoneNumber, VerificationCodeType type, string data = null)
        {
            string code = CodeGenerator.GenerateCode();

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
                await _twillioService.SendMessageAsync(phoneNumber, $"Your verification code: {code}");

            _logger.LogError($"Your verification code: {code}");
            return true;
        }
    }
}
