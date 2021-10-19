using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Notifications;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Send Push to user's Device
        /// </summary>
        /// <param name="user"></param>
        /// <param name="text"></param>
        Task SendPushNotification(string deviceToken, PushNotification notification);

        /// <summary>
        /// Send Push to user's Devices
        /// </summary>
        /// <param name="user"></param>
        /// <param name="text"></param>
        Task SendPushNotification(ApplicationUser user, PushNotification notification);
    }
}
