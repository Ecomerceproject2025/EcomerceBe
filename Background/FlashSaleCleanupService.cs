using EcomerceBE.Service.flashSale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EcomerceBE.Background
{
    /// <summary>
    /// Background task to cleanup expired flash sales periodically.
    /// </summary>
    public class FlashSaleCleanupService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // adjust if needed

        public FlashSaleCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Run immediately, then every interval
            _timer = new Timer(async _ => await RunCleanupAsync(), null, TimeSpan.Zero, _interval);
            return Task.CompletedTask;
        }

        private async Task RunCleanupAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var flashSaleService = scope.ServiceProvider.GetRequiredService<IFlashSaleService>();
                await flashSaleService.CleanupExpiredFlashSalesAsync();
            }
            catch
            {
                // swallow exceptions to avoid stopping the hosted service; logging can be added if needed
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}

