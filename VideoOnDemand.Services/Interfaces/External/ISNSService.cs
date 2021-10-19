using Amazon.SimpleNotificationService.Model;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface ISNSService
    {
        Task<PublishResponse> SendMessageAsync(string to, string message, string senderId = null);
    }
}
