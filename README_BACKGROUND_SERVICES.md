# Background Services

## 📋 Tổng quan

EcomerceBE sử dụng **Background Services** (IHostedService) để chạy các tác vụ định kỳ tự động, không cần user interaction. Các services này chạy trong background thread và tự động start khi ứng dụng khởi động.

## 🔄 Background Services

### FlashSaleCleanupService

**Location**: `Background/FlashSaleCleanupService.cs`

**Chức năng**: Tự động cleanup (xóa) các flash sales đã hết hạn

**Interval**: Mỗi 5 phút

**Implementation**:
```csharp
public class FlashSaleCleanupService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Chạy ngay lập tức, sau đó mỗi 5 phút
        _timer = new Timer(async _ => await RunCleanupAsync(), null, TimeSpan.Zero, _interval);
        return Task.CompletedTask;
    }

    private async Task RunCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var flashSaleService = scope.ServiceProvider.GetRequiredService<IFlashSaleService>();
        await flashSaleService.CleanupExpiredFlashSalesAsync();
    }
}
```

**Registration**:
```csharp
builder.Services.AddHostedService<FlashSaleCleanupService>();
```

**Dependencies**:
- `IFlashSaleService` - Flash sale service để cleanup

---

### EmbeddingGenerationService

**Location**: `Background/EmbeddingGenerationService.cs`

**Chức năng**: Tự động generate embeddings cho các products chưa có embedding

**Interval**: Mỗi 6 giờ

**Implementation**:
```csharp
public class EmbeddingGenerationService : IHostedService, IDisposable
{
    private readonly ILogger<EmbeddingGenerationService> _logger;
    private readonly IServiceProvider _services;
    private Timer? _timer;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Embedding Generation Service is starting.");
        
        // Chạy ngay lập tức lần đầu, sau đó mỗi 6 giờ
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
}
```

**Registration**:
```csharp
builder.Services.AddHostedService<EmbeddingGenerationService>();
```

**Dependencies**:
- `IEmbeddingService` - Embedding service để generate embeddings
- `ILogger<EmbeddingGenerationService>` - Logging

## ⚙️ Configuration

### Program.cs Registration

```csharp
// ✅ Background Services
builder.Services.AddHostedService<FlashSaleCleanupService>();
builder.Services.AddHostedService<EmbeddingGenerationService>();
```

### Service Provider Scope

Background services sử dụng `IServiceProvider.CreateScope()` để tạo scoped services:

```csharp
using var scope = _serviceProvider.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IService>();
await service.DoWorkAsync();
```

**Lý do**: DbContext và các scoped services cần được tạo trong scope riêng, không thể inject trực tiếp vào hosted service.

## 🔧 Customization

### Thay đổi Interval

Để thay đổi interval, sửa `_interval` trong constructor hoặc thêm vào configuration:

```csharp
// Option 1: Hardcode
private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

// Option 2: From configuration
private readonly TimeSpan _interval;

public FlashSaleCleanupService(IConfiguration configuration, ...)
{
    var minutes = configuration.GetValue<int>("BackgroundServices:FlashSaleCleanup:IntervalMinutes", 5);
    _interval = TimeSpan.FromMinutes(minutes);
}
```

### Thêm Background Service mới

1. **Tạo service class**:
```csharp
public class MyBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public MyBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, _interval);
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        using var scope = _serviceProvider.CreateScope();
        var myService = scope.ServiceProvider.GetRequiredService<IMyService>();
        await myService.DoWorkAsync();
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
```

2. **Register trong Program.cs**:
```csharp
builder.Services.AddHostedService<MyBackgroundService>();
```

## 📊 Monitoring

### Logging

Tất cả background services sử dụng `ILogger` để log:

```csharp
_logger.LogInformation("Service is starting.");
_logger.LogInformation($"Successfully processed {count} items.");
_logger.LogError(ex, "Error occurred executing service.");
```

### Health Checks (Optional)

Có thể thêm health checks để monitor background services:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<FlashSaleCleanupService>("flash_sale_cleanup", tags: new[] { "background" });
```

## 🐛 Troubleshooting

### Service không chạy

- Kiểm tra service đã được register trong `Program.cs`
- Kiểm tra application đang chạy (không phải build-only)
- Kiểm tra logs để xem có errors không

### Service chạy quá thường xuyên

- Kiểm tra `_interval` value
- Đảm bảo timer được dispose đúng cách

### Memory Leaks

- Đảm bảo `Dispose()` được implement
- Đảm bảo `Timer` được dispose khi service stop
- Đảm bảo scoped services được dispose sau khi dùng

## 📚 Related Documentation

- [Services](./README_SERVICES.md) - Services được sử dụng bởi background services
- [Flash Sale](./README_FLASH_SALE.md) - Flash sale cleanup details
- [AI/Recommendations](./ModelAI/README_BACKEND.md) - Embedding generation details

---

**Last Updated**: 2024-12-17

