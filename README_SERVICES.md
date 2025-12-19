# Services Architecture

## 📋 Tổng quan

Services layer trong EcomerceBE chứa business logic, tách biệt khỏi controllers và data access layer. Tất cả services được đăng ký trong `Program.cs` sử dụng Dependency Injection.

## 🏗️ Kiến trúc

```
Controllers (API Layer)
    ↓
Services (Business Logic Layer)
    ↓
DbContext (Data Access Layer)
    ↓
MySQL Database
```

## 📁 Services Structure

### Authentication Services

#### IAuthService / AuthService
**Location**: `Service/auth/`

**Methods**:
- `GenerateJwtToken(User user)` - Tạo JWT access token
- `GenerateRefreshToken()` - Tạo refresh token (32 bytes random)
- `GetPrincipalFromExpiredToken(string token)` - Parse expired token để refresh
- `HashPassword(string password)` - Hash password (PBKDF2)
- `VerifyPassword(string inputPassword, string storedHash)` - Verify password

**Dependencies**:
- `IConfiguration` - JWT settings

**Registration**:
```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
```

---

### Payment Services

#### IMoMoPaymentService / MoMoPaymentService
**Location**: `Service/Payment/`

**Methods**:
- `CreatePaymentRequestAsync(MoMoPaymentRequest request)` - Tạo payment request với MoMo
- `VerifyPaymentCallbackAsync(string signature, ...)` - Verify payment callback từ MoMo

**Dependencies**:
- `IConfiguration` - MoMo credentials
- `ILogger<MoMoPaymentService>` - Logging

**Registration**:
```csharp
builder.Services.AddScoped<IMoMoPaymentService, MoMoPaymentService>();
```

Xem chi tiết tại [README_PAYMENT.md](./README_PAYMENT.md).

---

### Flash Sale Services

#### IFlashSaleService / FlashSaleService
**Location**: `Service/flashSale/`

**Methods**:
- `GetAllFlashSalesWithItemsAsync()` - Lấy tất cả flash sales với items
- `CreateOrUpdateFlashSaleAsync(int? id, FlashSaleCreateDTO dto)` - Tạo/cập nhật flash sale
- `DeleteFlashSaleAsync(int id)` - Xóa flash sale
- `CleanupExpiredFlashSalesAsync()` - Xóa flash sales đã hết hạn

**Dependencies**:
- `AppDbContext` - Database access

**Registration**:
```csharp
builder.Services.AddScoped<IFlashSaleService, FlashSaleService>();
```

Xem chi tiết tại [README_FLASH_SALE.md](./README_FLASH_SALE.md).

---

### AI/Recommendation Services

#### IEmbeddingService / EmbeddingService
**Location**: `Service/ModelAI/`

**Methods**:
- `GenerateEmbeddingAsync(string text)` - Generate embedding từ text
- `GenerateEmbeddingsForAllProductsAsync()` - Generate embeddings cho tất cả products
- `GenerateEmbeddingForProductAsync(int productId)` - Generate embedding cho product cụ thể

**Dependencies**:
- `AppDbContext` - Database access
- `IHttpClientFactory` - HTTP client để gọi AI Model API
- `IConfiguration` - AI Model API URL

**Registration**:
```csharp
builder.Services.AddHttpClient(); // For EmbeddingService
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
```

---

#### IRecommendationService / RecommendationService
**Location**: `Service/ModelAI/`

**Methods**:
- `GetContentBasedRecommendationsAsync(int productId, int topK)` - Content-based recommendations
- `GetUserBasedRecommendationsAsync(int userId, int topK)` - User-based recommendations
- `CalculateCosineSimilarity(float[] vector1, float[] vector2)` - Tính cosine similarity

**Dependencies**:
- `AppDbContext` - Database access

**Registration**:
```csharp
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
```

Xem chi tiết tại [ModelAI/README_BACKEND.md](./ModelAI/README_BACKEND.md).

---

### User Services

#### UserService / IUserService
**Location**: `Service/user/`

**Methods**:
- `ParseAddress(string addressString)` - Parse address string thành Address object
- Các methods khác liên quan đến user management

**Dependencies**:
- `AppDbContext` - Database access

**Registration**:
```csharp
builder.Services.AddScoped<IUserService, UserService>();
```

---

### Email Service

#### Email
**Location**: `Service/Email.cs`

**Methods**:
- `SendEmailAsync(string toEmail, string subject, string body)` - Gửi email

**Dependencies**:
- `IConfiguration` - Email settings (SMTP)

**Usage**:
```csharp
var emailService = new Email(_configuration);
await emailService.SendEmailAsync("user@example.com", "Subject", "<html>...</html>");
```

Xem chi tiết tại [README_EMAIL_SERVICE.md](./README_EMAIL_SERVICE.md).

---

## 🔧 Dependency Injection

### Service Lifetime

#### Scoped (Recommended)
Services được tạo mới mỗi request:

```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFlashSaleService, FlashSaleService>();
```

**Sử dụng cho**: Services có state, cần access DbContext

#### Singleton
Service được tạo 1 lần duy nhất:

```csharp
builder.Services.AddSingleton<ICacheService, CacheService>();
```

**Sử dụng cho**: Stateless services, cache services

#### Transient
Service được tạo mới mỗi lần inject:

```csharp
builder.Services.AddTransient<IEmailService, EmailService>();
```

**Sử dụng cho**: Lightweight services, không cần state

### Registration trong Program.cs

```csharp
// ✅ DI Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFlashSaleService, FlashSaleService>();

// ✅ ModelAI Services
builder.Services.AddHttpClient(); // For EmbeddingService
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// ✅ Payment Services
builder.Services.AddScoped<IMoMoPaymentService, MoMoPaymentService>();
```

## 📝 Service Pattern

### Interface-First Design

Mỗi service có interface riêng:

```csharp
// Interface
public interface IAuthService
{
    string GenerateJwtToken(User user);
    string GenerateRefreshToken();
}

// Implementation
public class AuthService : IAuthService
{
    public string GenerateJwtToken(User user) { ... }
    public string GenerateRefreshToken() { ... }
}
```

### Constructor Injection

Services nhận dependencies qua constructor:

```csharp
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    
    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
}
```

### Usage trong Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel model)
    {
        var token = _authService.GenerateJwtToken(user);
        // ...
    }
}
```

## 🔄 Background Services

### FlashSaleCleanupService
**Location**: `Background/FlashSaleCleanupService.cs`

**Type**: `IHostedService`

**Chức năng**: Tự động cleanup flash sales đã hết hạn mỗi 5 phút

**Registration**:
```csharp
builder.Services.AddHostedService<FlashSaleCleanupService>();
```

---

### EmbeddingGenerationService
**Location**: `Background/EmbeddingGenerationService.cs`

**Type**: `IHostedService`

**Chức năng**: Tự động generate embeddings cho products mới mỗi 6 giờ

**Registration**:
```csharp
builder.Services.AddHostedService<EmbeddingGenerationService>();
```

Xem chi tiết tại [README_BACKGROUND_SERVICES.md](./README_BACKGROUND_SERVICES.md).

## 🧪 Testing Services

### Unit Testing

Services có thể được test độc lập bằng cách mock dependencies:

```csharp
// Mock IConfiguration
var mockConfig = new Mock<IConfiguration>();
mockConfig.Setup(c => c["Jwt:Key"]).Returns("test-key");

// Create service
var authService = new AuthService(mockConfig.Object);

// Test
var token = authService.GenerateJwtToken(user);
Assert.NotNull(token);
```

### Integration Testing

Test services với real database:

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;

using var context = new AppDbContext(options);
var flashSaleService = new FlashSaleService(context);

// Test
var result = await flashSaleService.GetAllFlashSalesWithItemsAsync();
Assert.NotNull(result);
```

## 📚 Related Documentation

- [API Controllers](./README_API_CONTROLLERS.md) - How controllers use services
- [Database](./README_DATABASE.md) - Database context used by services
- [Payment](./README_PAYMENT.md) - Payment service details
- [Flash Sale](./README_FLASH_SALE.md) - Flash sale service details
- [Background Services](./README_BACKGROUND_SERVICES.md) - Background services
- [AI/Recommendations](./ModelAI/README_BACKEND.md) - AI services details

---

**Last Updated**: 2024-12-17

