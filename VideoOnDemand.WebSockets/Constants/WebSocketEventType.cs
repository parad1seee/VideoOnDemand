using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.WebSockets.Constants
{
    public class WebSocketEventType
    {
        public const string Ping = "ping";

        public const string Pong = "pong";

        public const string Typing = "typing";

        public const string Message = "message";

        public const string NewNotification = "new_notification";

        public const string NewNotificationUpdateList = "new_notification_update_list";

        public const string NewTransaction = "new_transaction";

        public const string BalanceUpdated = "balance_updated";

        public const string MoneyInPlayUpdated = "money_in_play_updated";

        public const string NewMessage = "chat_new_message";

        public const string WagerUnreadMessagesUpdated = "chat_wager_unread_updated";

        public const string TotalUnreadMessagesUpdated = "chat_total_unread_updated";

        public const string MessageRead = "chat_message_read";

        public const string OnlineStatusChanged = "online_status_changed";
    }
}
