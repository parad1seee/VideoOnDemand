using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Models.Enums
{
    public enum StripeSubscriptionStatus
    {
        Incomplete,
        IncompleteExpired,
        Trialing,
        Active,
        PastDue,
        Canceled,
        Unpaid
    }
}
