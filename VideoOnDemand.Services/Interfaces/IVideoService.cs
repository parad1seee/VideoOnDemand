using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface IVideoService
    {
        Task Upload(IFormFile video);
    }
}
