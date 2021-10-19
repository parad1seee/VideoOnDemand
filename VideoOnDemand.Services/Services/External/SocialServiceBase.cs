using System.Net;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Domain.Entities.Identity;

namespace VideoOnDemand.Services.Services.External
{
    public abstract class SocialServiceBase
    {
        public virtual void BreakIfUserBlocked(ApplicationUser user)
        {
            if(!user.IsActive)
            {
                throw new CustomException(HttpStatusCode.Forbidden, "general", "Your account was blocked. For more information please email to following address: ");
            }
        }
    }
}
