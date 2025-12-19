# EcomerceBE - Backend API Documentation

## 📋 Tổng quan

EcomerceBE là backend API được xây dựng bằng **ASP.NET Core 9** và **C#**, cung cấp các dịch vụ cho ứng dụng e-commerce frontend. Hệ thống sử dụng **MySQL** làm database, **JWT** cho authentication, và tích hợp nhiều tính năng như payment gateway (MoMo), AI recommendations, flash sale, shipping management, và nhiều hơn nữa.

## 🚀 Quick Start

### Yêu cầu hệ thống

- **.NET SDK 9.0** hoặc cao hơn
- **MySQL Server** (5.7+ hoặc 8.0+)
- **Visual Studio 2022** hoặc **VS Code** với C# extension

### Cài đặt

1. **Clone repository và navigate vào thư mục backend:**
   ```bash
   cd EcomerceBE
   ```

2. **Cấu hình database trong `appsettings.json`:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "server=localhost;port=3306;database=EcomerceBE;user=root;password=YOUR_PASSWORD"
     }
   }
   ```

3. **Cài đặt dependencies:**
   ```bash
   dotnet restore
   ```

4. **Chạy migrations:**
   ```bash
   dotnet ef database update
   ```

5. **Chạy ứng dụng:**
   ```bash
   dotnet run
   ```

6. **Truy cập Swagger UI:**
   ```
   http://localhost:5099/swagger
   ```

## 📁 Cấu trúc Project

```
EcomerceBE/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── ProductController.cs
│   ├── OrderController.cs
│   ├── ModelAI/          # AI-related controllers
│   └── ...
├── Models/               # Entity Models
│   ├── Users/
│   ├── Products/
│   ├── Orders/
│   ├── ModelAI/
│   └── ...
├── DTOs/                 # Data Transfer Objects
│   ├── Auth/
│   ├── Products/
│   └── ...
├── Service/              # Business Logic Services
│   ├── auth/
│   ├── ModelAI/
│   ├── Payment/
│   └── ...
├── Data/                 # Database Context
│   └── AppDbContext.cs
├── Background/           # Background Services
│   ├── FlashSaleCleanupService.cs
│   └── EmbeddingGenerationService.cs
├── Migrations/           # EF Core Migrations
├── Program.cs            # Application Entry Point
└── appsettings.json      # Configuration
```

## 🔧 Cấu hình

### JWT Authentication

Cấu hình trong `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "http://localhost:5099",
    "Audience": "http://localhost:5099"
  }
}
```

### Google OAuth

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  }
}
```

### MoMo Payment

```json
{
  "MoMo": {
    "PartnerCode": "MOMO",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "ApiEndpoint": "https://test-payment.momo.vn/v2/gateway/api/create",
    "ReturnUrl": "http://localhost:5099/api/payment/momo/return",
    "NotifyUrl": "http://localhost:5099/api/payment/momo/notify"
  }
}
```

### AI Model API

```json
{
  "AIModel": {
    "ApiUrl": "http://localhost:8000"
  }
}
```

### Email Settings

```json
{
  "EmailSettings": {
    "From": "your-email@gmail.com",
    "Password": "your-app-password",
    "Host": "smtp.gmail.com",
    "Port": 587,
    "AdminEmail": "admin@example.com"
  }
}
```

## 📚 Tài liệu chi tiết

Dưới đây là danh sách các tài liệu chi tiết được sắp xếp theo thứ tự khuyến nghị để nghiên cứu:

### 1. [Authentication & Authorization](./README_AUTHENTICATION.md)
Hướng dẫn chi tiết về JWT authentication, Google OAuth, refresh token mechanism, và role-based authorization.

### 2. [Database & Models](./README_DATABASE.md)
Tổng quan về database schema, Entity Framework Core configuration, relationships, và migrations.

### 3. [API Controllers](./README_API_CONTROLLERS.md)
Tổng quan về tất cả API endpoints, request/response formats, và authentication requirements.

### 4. [Services Architecture](./README_SERVICES.md)
Kiến trúc services layer, dependency injection, và các service implementations.

### 5. [Payment Integration - MoMo](./README_PAYMENT.md)
Hướng dẫn tích hợp MoMo QR payment, payment flow, và callback handling.

### 6. [Background Services](./README_BACKGROUND_SERVICES.md)
Các background services như Flash Sale cleanup và Embedding generation.

### 7. [Shipping Module](./README_SHIPPING.md)
Quản lý shipping methods, shipper accounts, và order shipping tracking.

### 8. [Flash Sale](./README_FLASH_SALE.md)
Hệ thống flash sale với tự động cleanup và quản lý inventory.

### 9. [Email Service](./README_EMAIL_SERVICE.md)
Cấu hình và sử dụng email service để gửi notifications.

### 10. [AI/Recommendations](./ModelAI/README_BACKEND.md)
Hệ thống AI recommendations với content-based và user-based filtering.

## 🔐 Authentication Flow

### Login Flow

1. Client gửi credentials → `POST /api/auth/login`
2. Server verify password → Generate JWT access token + refresh token
3. Client lưu tokens → Sử dụng access token cho các request tiếp theo

### Refresh Token Flow

1. Access token hết hạn → Client gửi refresh token → `POST /api/auth/refresh`
2. Server verify refresh token → Generate access token mới
3. Client cập nhật access token

### Google OAuth Flow

1. Client redirect user đến Google → User authorize
2. Google redirect về với authorization code
3. Client gửi code → `POST /api/auth/google-login`
4. Server verify code → Tạo/login user → Trả về JWT tokens

Chi tiết xem tại [README_AUTHENTICATION.md](./README_AUTHENTICATION.md).

## 🛠️ Development

### Chạy Migrations

```bash
# Tạo migration mới
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Xóa migration cuối cùng
dotnet ef migrations remove
```

### Seed Data

Admin users được tự động seed khi ứng dụng khởi động (xem `AppDbContext.SeedAdminUsers()`).

Default admin accounts:
- Email: `Hungv5996@gmail.com` / Password: `admin@1234`
- Email: `thaithanhphat323@gmail.com` / Password: `admin@1234`

### Testing với Swagger

1. Mở Swagger UI: `http://localhost:5099/swagger`
2. Click "Authorize" → Nhập JWT token (format: `Bearer YOUR_TOKEN`)
3. Test các endpoints

## 📦 Dependencies chính

- **Microsoft.EntityFrameworkCore** - ORM
- **Pomelo.EntityFrameworkCore.MySql** - MySQL provider
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT authentication
- **Google.Apis.Auth** - Google OAuth
- **MailKit** - Email sending
- **BCrypt.Net-Next** - Password hashing
- **EPPlus** - Excel processing
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI

## 🌐 API Base URL

- **Development**: `http://localhost:5099/api`
- **Swagger UI**: `http://localhost:5099/swagger`

## 🔍 Troubleshooting

### Lỗi kết nối Database

- Kiểm tra connection string trong `appsettings.json`
- Đảm bảo MySQL server đang chạy
- Kiểm tra user/password có đúng không

### Lỗi JWT Token

- Kiểm tra `Jwt:Key` trong `appsettings.json` không được để trống
- Đảm bảo token được gửi đúng format: `Authorization: Bearer TOKEN`

### Lỗi Migration

- Xóa migration cũ: `dotnet ef migrations remove`
- Tạo lại: `dotnet ef migrations add MigrationName`
- Apply: `dotnet ef database update`

## 📝 Notes

- Tất cả timestamps sử dụng UTC (`DateTime.UtcNow`)
- Decimal precision: `decimal(18,2)` cho tất cả giá tiền
- CORS được cấu hình để allow all origins (development only)
- Background services chạy tự động khi ứng dụng khởi động

## 🤝 Contributing

Xem [CONTRIBUTING.md](../E_Com_FE/Ecom_app/CONTRIBUTING.md) cho guidelines về branch naming, PR naming, và required PR content.

## 📄 License

Xem [LICENSE](../LICENSE) file.

---

**Last Updated**: 2024-12-17

