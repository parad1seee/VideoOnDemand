using Microsoft.Extensions.DependencyInjection;
using VideoOnDemand.WebSockets.Managers;
using VideoOnDemand.WebSockets.Handlers;

namespace VideoOnDemand.WebSockets.Extentions
{
    public static class Extentions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddTransient(typeof(WebSocketConnectionManager<>));

            services.AddSingleton(typeof(WebSocketMessageHandler));

            return services;
        }
    }
}
