using EcomerceBE.Service.ModelAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcomerceBE.Background
{
    /// <summary>
    /// Background service để tự động generate embeddings cho sản phẩm mới
    /// Chạy định kỳ để đảm bảo tất cả sản phẩm đều có embedding
    /// </summary>
    public class EmbeddingGenerationService : IHostedService, IDisposable
    {
        private readonly ILogger<EmbeddingGenerationService> _logger;
        private readonly IServiceProvider _services;
        private Timer? _timer;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Chạy mỗi 6 giờ

        public EmbeddingGenerationService(
            ILogger<EmbeddingGenerationService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Embedding Generation Service is starting.");
            
            // Chạy ngay lập tức lần đầu, sau đó chạy định kỳ
            _timer = new Timer(DoWork, null, TimeSpan.Zero, _interval);
            
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            _logger.LogInformation("Embedding Generation Service is doing work.");

            using (var scope = _services.CreateScope())
            {
                try
                {
                    var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                    
                    // Generate embeddings cho tất cả sản phẩm chưa có embedding
                    var count = await embeddingService.GenerateEmbeddingsForAllProductsAsync();
                    
                    if (count > 0)
                    {
                        _logger.LogInformation($"Successfully generated {count} embeddings.");
                    }
                    else
                    {
                        _logger.LogInformation("All products already have embeddings.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Embedding Generation Service.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Embedding Generation Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}

