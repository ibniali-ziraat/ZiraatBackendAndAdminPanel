using Microsoft.Extensions.Logging;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.SignalR.RealTime;

namespace ECommerce.Module.SignalR.Hubs
{
    public class CommonHub : OnlineClientHubBase
    {
        public CommonHub(
            IWorkContext workContext,
            IOnlineClientManager onlineClientManager,
            ILogger<CommonHub> logger) : base(workContext, onlineClientManager)
        {
            Logger = logger;
        }

        public object Register()
        {
            var onlineClient = OnlineClientManager.GetByConnectionIdOrNull(Context.ConnectionId);
            var isAuthenticated = Context.User?.Identity?.IsAuthenticated == true;

            Logger.LogInformation(
                "SignalR client registered. ConnectionId: {ConnectionId}, UserId: {UserId}, IsAuthenticated: {IsAuthenticated}",
                Context.ConnectionId,
                onlineClient?.UserId,
                isAuthenticated);

            return new
            {
                Context.ConnectionId,
                UserId = onlineClient?.UserId,
                IsAuthenticated = isAuthenticated
            };
        }
    }
}
