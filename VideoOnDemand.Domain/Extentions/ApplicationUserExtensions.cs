using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Domain.Entities.Identity;
using System.Net;

namespace VideoOnDemand.Domain.Extentions
{
    public static class ApplicationUserExtensions
    {
        public static void ThrowIfBlocked(this ApplicationUser user)
        {
            if(!user.IsActive)
                if (!user.IsActive)
                    throw new CustomException(HttpStatusCode.Forbidden, "general", "Your account has been blocked. For more information please email administrator at: ");
        }
    }
}
