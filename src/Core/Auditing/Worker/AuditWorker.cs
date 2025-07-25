using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

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

        void IAuditEntryContainer.AddEntryForBackgrondSave(IAuditEntry entry)
        {
            _channel.Writer.TryWrite(entry);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                // the Channel.ReadAllAsync waits until a new message is arrived, so the Thread.Sleep is not neccesary
                await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await entry.SaveEntry();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError( ex.ToString());
                    }
                }
            }, stoppingToken);
        }
    }

    public static class AuditWorkerExtensions
    {

        public static void AddAuditWorker(this IServiceCollection services)
        {
            // add audit worker itself
            services.AddSingleton<AuditWorker>();
            // add for implement the IAuditEntryContainer service
            services.AddSingleton<IAuditEntryContainer>(sp => sp.GetRequiredService<AuditWorker>());
            // add for background service
            services.AddHostedService(sp => sp.GetRequiredService<AuditWorker>());            
        }
    }
}