# Authentication & Authorization

## 📋 Tổng quan

Hệ thống authentication của EcomerceBE sử dụng **JWT (JSON Web Tokens)** cho stateless authentication, hỗ trợ **Google OAuth** cho đăng nhập nhanh, và **refresh token mechanism** để đảm bảo bảo mật và trải nghiệm người dùng tốt.

## 🔐 Kiến trúc Authentication

### Components

1. **AuthController** (`Controllers/AuthController.cs`) - API endpoints
2. **AuthService** (`Service/auth/AuthService.cs`) - Business logic
3. **JWT Middleware** - Configured trong `Program.cs`
4. **Role-based Authorization** - Admin/User roles

### Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    REGISTRATION FLOW                        │
└─────────────────────────────────────────────────────────────┘
Client → POST /api/auth/register
         { email, password, name }
         ↓
Backend → Hash password (PBKDF2/BCrypt)
         → Create User (Role: "User")
         → Return 200 OK

┌─────────────────────────────────────────────────────────────┐
│                      LOGIN FLOW                              │
└─────────────────────────────────────────────────────────────┘
Client → POST /api/auth/login
         { email, password }
         ↓
Backend → Verify password (BCrypt/PBKDF2)
         → Generate JWT Access Token (1 hour expiry)
         → Generate Refresh Token (7 days expiry)
         → Save refresh token to DB
         → Return { token, refreshToken }

┌─────────────────────────────────────────────────────────────┐
│                   REFRESH TOKEN FLOW                         │
└─────────────────────────────────────────────────────────────┘
Client → POST /api/auth/refresh-token
         { token: expired_access_token, refreshToken }
         ↓
Backend → Validate expired token signature
         → Find user by email/ID from token claims
         → Verify refresh token matches DB
         → Check refresh token not expired
         → Generate new access token
         → Generate new refresh token
         → Update DB
         → Return { token, refreshToken }

┌─────────────────────────────────────────────────────────────┐
│                   GOOGLE OAUTH FLOW                          │
└─────────────────────────────────────────────────────────────┘
Client → User clicks "Sign in with Google"
         → Google OAuth consent screen
         → Google returns ID token
         ↓
Client → POST /api/auth/google-login
         { idToken: google_id_token }
         ↓
Backend → Validate ID token với Google
         → Extract email, name from payload
         → Find or create User
         → Generate JWT tokens
         → Return { token, refreshToken }
```

## 🔑 JWT Token Structure

### Access Token Claims

```json
{
  "email": "user@example.com",
  "role": "User",
  "id": "123",
  "iss": "http://localhost:5099",
  "aud": "http://localhost:5099",
  "exp": 1234567890,
  "iat": 1234567890
}
```

### Token Expiry

- **Access Token**: 1 hour
- **Refresh Token**: 7 days

## 📁 Files liên quan

### Controllers

- **`Controllers/AuthController.cs`**
  - `POST /api/auth/register` - Đăng ký tài khoản
  - `POST /api/auth/login` - Đăng nhập
  - `POST /api/auth/refresh-token` - Refresh access token
  - `POST /api/auth/google-login` - Google OAuth login
  - `POST /api/auth/send-otp` - Gửi OTP qua email
  - `POST /api/auth/verify-otp` - Verify OTP và reset password
  - `POST /api/auth/sendEmailVerify` - Gửi email verification
  - `POST /api/auth/verification/confirm` - Xác nhận email
  - `POST /api/auth/change_profile` - Cập nhật profile

### Services

- **`Service/auth/IAuthService.cs`** - Interface
- **`Service/auth/AuthService.cs`** - Implementation
  - `GenerateJwtToken(User user)` - Tạo JWT token
  - `GenerateRefreshToken()` - Tạo refresh token (32 bytes random)
  - `GetPrincipalFromExpiredToken(string token)` - Parse expired token
  - `HashPassword(string password)` - Hash password (PBKDF2)
  - `VerifyPassword(string inputPassword, string storedHash)` - Verify password

### Models

- **`Models/Users/User.cs`**
  - `RefreshToken` - Refresh token string
  - `RefreshTokenExpiryTime` - Expiry datetime
  - `EmailVerificationToken` - Token để verify email
  - `EmailVerificationExpiry` - Expiry datetime
  - `status` - "Active" hoặc "Pending"

- **`Models/Auth/OtpCode.cs`**
  - `Email` - Email address
  - `Code` - 6-digit OTP
  - `Expiration` - Expiry datetime (5 minutes)

### DTOs

- **`DTOs/Auth/RegisterModel.cs`**
- **`DTOs/Auth/LoginModel.cs`**
- **`DTOs/Auth/RefreshRequest.cs`**
- **`DTOs/Auth/GoogleLoginRequest.cs`**
- **`DTOs/Auth/OtpVerifyRequest.cs`**
- **`DTOs/Auth/ConfirmEmailRequest.cs`**
- **`DTOs/Auth/ChangeProfile.cs`**

## ⚙️ Cấu hình

### appsettings.json

```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-characters",
    "Issuer": "http://localhost:5099",
    "Audience": "http://localhost:5099"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  },
  "AppUrls": {
    "FrontendBaseUrl": "http://localhost:3000"
  }
}
```

### Program.cs Configuration

```csharp
// JWT Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();
```

## 🔒 Password Hashing

Hệ thống hỗ trợ **2 loại password hashing**:

1. **BCrypt** (khuyến nghị)
   - Format: `$2a$...` hoặc `$2b$...`
   - Sử dụng `BCrypt.Net.BCrypt.HashPassword()` và `Verify()`

2. **PBKDF2** (legacy)
   - Format: Base64 string
   - Sử dụng `AuthService.HashPassword()` và `VerifyPassword()`

**Lưu ý**: Khi login, hệ thống tự động detect loại hash dựa trên prefix `$2` để chọn phương thức verify phù hợp.

## 🌐 Google OAuth Setup

### 1. Tạo Google OAuth Credentials

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo project mới hoặc chọn project hiện có
3. Enable **Google+ API**
4. Tạo **OAuth 2.0 Client ID**:
   - Application type: **Web application**
   - Authorized JavaScript origins: `http://localhost:3000`
   - Authorized redirect URIs: `http://localhost:3000/auth/google/callback`

### 2. Cấu hình Backend

Thêm credentials vào `appsettings.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
  }
}
```

### 3. Frontend Integration

Frontend sử dụng `@react-oauth/google` để lấy ID token, sau đó gửi đến backend endpoint `/api/auth/google-login`.

Xem chi tiết tại [README_GOOGLE_AUTH.md](../E_Com_FE/Ecom_app/README_GOOGLE_AUTH.md) (Frontend).

## 📧 Email Verification & OTP

### Email Verification Flow

1. User đăng ký → Status = "Pending"
2. User request verification → `POST /api/auth/sendEmailVerify`
3. Backend generate token → Gửi email với verification link
4. User click link → Frontend gọi `POST /api/auth/verification/confirm`
5. Backend verify token → Update status = "Active"

### OTP Flow (Password Reset)

1. User request OTP → `POST /api/auth/send-otp` với email
2. Backend generate 6-digit OTP → Lưu vào DB (expiry: 5 minutes)
3. Backend gửi email với OTP code
4. User submit OTP + new password → `POST /api/auth/verify-otp`
5. Backend verify OTP → Update password → Delete OTP record

### Email Template

OTP email sử dụng HTML template với:
- Logo từ `/assets/logo/logo-email.png`
- Styled OTP code display
- Footer với thông tin liên hệ

## 🛡️ Authorization

### Role-based Authorization

Sử dụng `[Authorize(Roles = "Admin")]` attribute trên controllers/actions:

```csharp
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    // Only Admin can access
}
```

### JWT Claims

- `ClaimTypes.Email` - User email
- `"role"` - User role ("Admin" hoặc "User")
- `"id"` - User ID

### Example: Get Current User

```csharp
[Authorize]
[HttpGet("profile")]
public IActionResult GetProfile()
{
    var email = User.FindFirstValue(ClaimTypes.Email);
    var role = User.FindFirstValue("role");
    var userId = int.Parse(User.FindFirstValue("id"));
    
    // ... get user from DB
}
```

## 📝 API Endpoints

### Register

**Endpoint**: `POST /api/auth/register`

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "password123",
  "name": "John Doe"
}
```

**Response**: `200 OK` với message "Đăng ký thành công"

**Status Codes**:
- `200 OK` - Success
- `400 Bad Request` - Email đã tồn tại

---

### Login

**Endpoint**: `POST /api/auth/login`

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-random-string"
}
```

**Status Codes**:
- `200 OK` - Success
- `401 Unauthorized` - Sai email/password

---

### Refresh Token

**Endpoint**: `POST /api/auth/refresh-token`

**Authentication**: Not required (AllowAnonymous)

**Request Body**:
```json
{
  "token": "expired_access_token",
  "refreshToken": "refresh_token_from_db"
}
```

**Response**: (giống Login)

**Status Codes**:
- `200 OK` - Success
- `401 Unauthorized` - Invalid token hoặc refresh token expired

---

### Google Login

**Endpoint**: `POST /api/auth/google-login`

**Request Body**:
```json
{
  "idToken": "google_id_token_from_frontend"
}
```

**Response**: (giống Login)

**Status Codes**:
- `200 OK` - Success
- `401 Unauthorized` - Invalid Google token

---

### Send OTP

**Endpoint**: `POST /api/auth/send-otp`

**Request Body**: `"user@example.com"` (string)

**Response**: `200 OK` với message "Đã gửi OTP qua email"

**Status Codes**:
- `200 OK` - Success
- `404 Not Found` - Email không tồn tại

---

### Verify OTP & Reset Password

**Endpoint**: `POST /api/auth/verify-otp`

**Request Body**:
```json
{
  "email": "user@example.com",
  "code": "123456",
  "newPassword": "newpassword123"
}
```

**Response**: `200 OK` với message "Đổi mật khẩu thành công"

**Status Codes**:
- `200 OK` - Success
- `400 Bad Request` - OTP không hợp lệ hoặc đã hết hạn
- `404 Not Found` - User không tồn tại

---

### Send Email Verification

**Endpoint**: `POST /api/auth/sendEmailVerify`

**Request Body**: `"user@example.com"` (string)

**Response**:
```json
{
  "message": "Nếu email tồn tại, hệ thống đã gửi hướng dẫn xác nhận.",
  "verificationLink": "http://localhost:3000/verify-email?email=...&token=..."
}
```

---

### Confirm Email Verification

**Endpoint**: `POST /api/auth/verification/confirm`

**Request Body**:
```json
{
  "email": "user@example.com",
  "token": "base64-url-encoded-token"
}
```

**Response**: `200 OK` với message "Xác nhận email thành công"

---

### Change Profile

**Endpoint**: `POST /api/auth/change_profile`

**Authentication**: Required

**Request Body**:
```json
{
  "id": 123,
  "fname": "John",
  "lname": "Doe",
  "avatar": "https://...",
  "currentPassWord": "oldpassword",
  "newPassWord": "newpassword",
  "address": { ... }
}
```

**Response**: `200 OK` với message "Profile updated successfully"

## 🔍 Testing

### Test với Swagger

1. Mở Swagger UI: `http://localhost:5099/swagger`
2. Test `/api/auth/register` → Lấy response
3. Test `/api/auth/login` → Lấy `token` và `refreshToken`
4. Click "Authorize" → Nhập `Bearer YOUR_TOKEN`
5. Test các protected endpoints

### Test với cURL

```bash
# Register
curl -X POST http://localhost:5099/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"test123","name":"Test User"}'

# Login
curl -X POST http://localhost:5099/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"test123"}'

# Refresh Token
curl -X POST http://localhost:5099/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{"token":"EXPIRED_TOKEN","refreshToken":"REFRESH_TOKEN"}'
```

## 🐛 Troubleshooting

### Lỗi: "Invalid token"

- Kiểm tra `Jwt:Key` trong `appsettings.json` không được để trống
- Đảm bảo token được gửi đúng format: `Authorization: Bearer TOKEN`
- Kiểm tra token chưa hết hạn (access token: 1 hour)

### Lỗi: "Invalid refresh token"

- Kiểm tra refresh token đã được lưu vào DB sau khi login
- Kiểm tra refresh token chưa hết hạn (7 days)
- Đảm bảo refresh token trong request khớp với DB

### Lỗi: "Token Google không hợp lệ"

- Kiểm tra `Authentication:Google:ClientId` trong `appsettings.json`
- Đảm bảo Google OAuth credentials đúng
- Kiểm tra ID token từ frontend còn valid

### Lỗi: "Email đã tồn tại"

- Email phải unique trong hệ thống
- Nếu muốn đăng nhập với Google, user phải đã đăng ký với email đó trước

## 📚 Related Documentation

- [Frontend Auth Feature](../E_Com_FE/Ecom_app/README_AUTH_FEATURE.md)
- [Google OAuth Client Config](../E_Com_FE/Ecom_app/README_GOOGLE_AUTH.md)
- [API Client Configuration](../E_Com_FE/Ecom_app/README_API_CLIENT.md)

---

**Last Updated**: 2024-12-17

