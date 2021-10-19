using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Domain.Entities.Logging;
using VideoOnDemand.Services.Interfaces.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.External
{
    public class SNSService : ISNSService
    {
        private IConfiguration _configuration;
        private ILogger<SNSService> _logger;

        public SNSService(IConfiguration configuration, ILogger<SNSService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PublishResponse> SendMessageAsync(string to, string message, string senderId = null)
        {
            using(var client = new AmazonSimpleNotificationServiceClient(_configuration["AWS:AccessKey"], _configuration["AWS:SecretKey"], Amazon.RegionEndpoint.EUWest1))
            {
                var request = new PublishRequest
                {
                    PhoneNumber = to,
                    Message = message
                };
                request.MessageAttributes.Add("AWS.SNS.SMS.SMSType", new MessageAttributeValue { DataType = "String", StringValue = "Transactional" });
                if(senderId != null)
                {
                    request.MessageAttributes.Add("AWS.SNS.SMS.SenderId", new MessageAttributeValue { DataType = "String", StringValue = senderId });
                }

                try
                {
                    var response = await client.PublishAsync(request);
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError("AWS SNS Error: " + ex.InnerException?.Message ?? ex.Message);
                    throw;
                }
            }
        }
    }
}
