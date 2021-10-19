using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface ITwillioService
    {
        Task SendMessageAsync(string to, string body);

        Task<object> Call(string to);
    }
}
