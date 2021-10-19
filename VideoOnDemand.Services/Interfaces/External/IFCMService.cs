using VideoOnDemand.Models.Notifications;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface IFCMService
    {
        /// <summary>
        /// Send Push Notification to device
        /// </summary>
        /// <param name="deviceTokens">List of all devices assigned to a user</param>
        /// <param name="title">Title of notification</param>
        /// <param name="body">Description of notification</param>
        /// <returns></returns>
        Task<bool> SendPushNotification(string deviceToken, PushNotification notification, int badge = 0, bool testMode = false);
    }
}
