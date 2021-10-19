using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Notifications;
using VideoOnDemand.ScheduledTasks;
using VideoOnDemand.ScheduledTasks.OnTime;
using VideoOnDemand.Services.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Jobs
{
    public class OneWeekAfterRegistration: OnTimeTask<int>, IScheduledTask, IRestorableTask
    {
        private const string LOG_IDENTIFIER = "OneWeekAfterRegistration";

        private ILogger<OneWeekAfterRegistration> _logger;

        private IServiceProvider _serviceProvider;

        public OneWeekAfterRegistration(IServiceProvider serviceProvider, ILogger<OneWeekAfterRegistration> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation($"{LOG_IDENTIFIER} => started. At {DateTime.UtcNow.ToShortTimeString()}");
        }

        public override DateTime AdjustDate(DateTime date, bool reverse = false)
        {
            var delta = (reverse) ? -7 : 7;

            return base.AdjustDate(date).AddDays(delta);
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

                    // check users 
                    var usersToNotify = unitOfWork.Repository<ApplicationUser>().Get(x => x.IsActive && !x.IsDeleted && RunList.Contains(x.Id) && x.Devices.Any(w => w.IsVerified && w.IsActive))
                        .TagWith(nameof(ExecuteAsync) + "_GetUsersToNotify")
                        .Include(x => x.Devices);

                    var notification = new PushNotification("Registration", null);

                    foreach (var user in usersToNotify)
                    {
                        notification.Title = $"Thank you, {user.UserName}! You were beeing with us for one week!";
                        await notificationService.SendPushNotification(user, notification);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{LOG_IDENTIFIER} => Exception.Message: {ex.Message}");
            }
        }

        /// <summary>
        /// Recalculate which tasks should be runned after server starts
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RestoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // TODO: Example code! Extend target service interface with new method!
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var date = AdjustDate(DateTime.UtcNow, true);

                    var usersToNotify = unitOfWork.Repository<ApplicationUser>().Get(x => x.IsActive && !x.IsDeleted && x.RegistratedAt >= date)
                        .TagWith(nameof(RestoreAsync) + "_GetUsersToNotify");

                    foreach (var user in usersToNotify)
                    {
                        Add(user.Id, user.RegistratedAt);
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
