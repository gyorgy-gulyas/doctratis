using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Auditing.Worker
{
    public class AuditWorker : BackgroundService, IAuditEntryContainer
    {
        private readonly ILogger _logger;
        private readonly Channel<IAuditEntry> _channel = Channel.CreateUnbounded<IAuditEntry>();

        public AuditWorker( ILogger<AuditWorker> logger )
        {
            _logger = logger;
        }

        void IAuditEntryContainer.AddEntry(IAuditEntry entry)
        {
            _channel.Writer.TryWrite(entry);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(async () =>
            {
                await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await entry.SaveEntry();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error( ex.ToString());
                    }
                }
            }, stoppingToken);
        }
    }
}