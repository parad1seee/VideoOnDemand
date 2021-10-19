using VideoOnDemand.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface ICallService
    {
        Task VerificationCall(ApplicationUser user);
    }
}
