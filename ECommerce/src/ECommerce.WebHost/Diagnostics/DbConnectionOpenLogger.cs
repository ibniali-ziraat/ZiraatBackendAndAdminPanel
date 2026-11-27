using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.WebHost.Diagnostics
{
    public class DbConnectionOpenLogger : DbConnectionInterceptor
    {
        private readonly ILogger<DbConnectionOpenLogger> _logger;

        public DbConnectionOpenLogger(ILogger<DbConnectionOpenLogger> logger)
        {
            _logger = logger;
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            _logger.LogWarning("DB Connection opened: {ConnectionString}", connection.ConnectionString);
            base.ConnectionOpened(connection, eventData);
        }

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("DB Connection opened (async): {ConnectionString}", connection.ConnectionString);
            return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }
    }
}
