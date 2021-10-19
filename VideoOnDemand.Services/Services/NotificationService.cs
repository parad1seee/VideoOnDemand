using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Chat;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Notifications;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using System.Linq;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services
{
    public class NotificationService : INotificationService
    {
        private IUnitOfWork _unitOfWork;
        private IFCMService _FCMService;
        private ILogger<NotificationService> _logger;
        private IMapper _mapper;

        public NotificationService(IFCMService FCMService, ILogger<NotificationService> logger, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _FCMService = FCMService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task SendPushNotification(string deviceToken, PushNotification notification)
        {
            await _FCMService.SendPushNotification(deviceToken, notification);
        }

        public async Task SendPushNotification(ApplicationUser user, PushNotification notification)
        {
            // Get all active verified devices
            var devices = user.Devices?.Where(x => x.IsActive && x.IsVerified) ?? _unitOfWork.Repository<UserDevice>().Get(x => x.UserId == user.Id && x.IsActive && x.IsVerified).TagWith(nameof(SendPushNotification) + "_GetDeviceTokens").ToList();

            var chat = _unitOfWork.Repository<Chat>().Get(x => x.Users.Any(y => y.UserId == user.Id && y.IsActive))
                .TagWith(nameof(SendPushNotification) + "_GetBadge")
                .Include(x => x.Users)
                .Include(x => x.Messages)
                .FirstOrDefault();

            if (chat != null) 
            { 
                var badge = chat.GetBadge(user.Id);

                // Send notification to all user devices
                foreach (var device in devices)
                {
                    await _FCMService.SendPushNotification(device.DeviceToken, notification, badge);
                } 
            }
        }
    }
}
