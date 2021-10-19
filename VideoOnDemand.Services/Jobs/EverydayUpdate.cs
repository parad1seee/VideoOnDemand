using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Notifications;
using VideoOnDemand.ScheduledTasks;
using VideoOnDemand.ScheduledTasks.Schedule;
using VideoOnDemand.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Jobs
{
    public class EverydayUpdate : ScheduledTask, IScheduledTask
    {
        private const string LOG_IDENTIFIER = "EverydayUpdate";

        private ILogger<EverydayUpdate> _logger;

        private IServiceProvider _serviceProvider;

        public EverydayUpdate(IServiceProvider serviceProvider, ILogger<EverydayUpdate> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation($"{LOG_IDENTIFIER} => started. At {DateTime.UtcNow.ToShortTimeString()}");

            //run everyday
            Schedule = "0 0 */1 * *";
            _nextRunTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Task which will be executed on schedule
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // TODO: Example code! Extend target service interface with new method!

                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var usersToNotify = unitOfWork.Repository<ApplicationUser>().Get(x => x.IsActive && !x.IsDeleted /*&& x.Devices.Any(w => w.IsVerified && w.IsActive)*/)
                        .TagWith(nameof(ExecuteAsync) + "_GetUsersToNotify")
                        .Include(x => x.Devices);

                    var notification = new PushNotification("Everyday info", null);

                    foreach (var user in usersToNotify)
                    {
                        notification.Title = $"Nice to have you here, {user.UserName}!";
                        await notificationService.SendPushNotification(user, notification);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{LOG_IDENTIFIER} => Exception.Message: {ex.Message}");
            }
        }
    }
}
