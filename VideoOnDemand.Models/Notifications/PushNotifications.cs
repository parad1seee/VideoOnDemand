
namespace VideoOnDemand.Models.Notifications
{
    public static class PushNotifications
    {
        #region Chat notifications

        public static PushNotification NewMessage(int userId, string senderName, string messageText, int chatId)
        {
            return new PushNotification($"New message from {senderName}", messageText, new PushNotificationData(userId, NotificationActionType.RedirectToChat, $"chat_{chatId}"));
        }

        public static PushNotification NewMessages(int userId, string senderName, int messageCount, int chatId)
        {
            return new PushNotification($"New messages from {senderName}", $"{messageCount} new messages", new PushNotificationData(userId, NotificationActionType.RedirectToChat, $"chat_{chatId}"));
        }

        #endregion
    }
}