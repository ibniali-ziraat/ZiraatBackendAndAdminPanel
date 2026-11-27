using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.SignalR.RealTime;

namespace ECommerce.Module.SignalR.Hubs
{
    public abstract class OnlineClientHubBase : HubBase
    {
        protected IOnlineClientManager OnlineClientManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonHub"/> class.
        /// </summary>
        protected OnlineClientHubBase(IWorkContext workContext, IOnlineClientManager onlineClientManager) : base(workContext)
        {
            OnlineClientManager = onlineClientManager;
            Logger = NullLogger<OnlineClientHubBase>.Instance;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var client = CreateClientForCurrentConnection();

            Logger.LogDebug("A client is connected: " + client);

            OnlineClientManager.Add(client);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);

            Logger.LogDebug("A client is disconnected: " + Context.ConnectionId);

            try
            {
                OnlineClientManager.Remove(Context.ConnectionId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex.ToString(), ex);
            }
        }

        protected virtual IOnlineClient CreateClientForCurrentConnection()
        {
            var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Context.User?.FindFirstValue("sub")
                ?? Context.User?.FindFirstValue("id");

            return new OnlineClient(
                Context.ConnectionId,
                long.TryParse(userIdValue, out var userId) ? userId : null);
        }
    }
}
