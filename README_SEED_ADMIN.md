# Seed Admin Users - Hướng dẫn

## 📋 Tổng quan

Hệ thống tự động seed admin users khi ứng dụng khởi động lần đầu. Điều này đảm bảo luôn có ít nhất một admin account để quản lý hệ thống.

## ⚙️ Cơ chế hoạt động

### Tự động Seed khi Startup

Trong `Program.cs`, khi ứng dụng khởi động:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.SeedAdminUsers(); // Tự động seed admin users
    // ...
}
```

### Logic Seed trong AppDbContext

Method `SeedAdminUsers()` trong `Data/AppDbContext.cs`:

1. Kiểm tra xem admin user đã tồn tại chưa (dựa trên email)
2. Nếu chưa tồn tại → Tạo admin user mới với password đã hash
3. Lưu vào database

## 🔐 Tạo Admin User mới

### Cách 1: Sử dụng Seed Method (Khuyến nghị)

**Bước 1**: Mở `Data/AppDbContext.cs` và tìm method `SeedAdminUsers()`

**Bước 2**: Thêm logic để seed admin user mới:

```csharp
public void SeedAdminUsers()
{
    // Admin user 1
    if (!Users.Any(u => u.Email == "your-admin-email@example.com"))
    {
        var adminPass = BCrypt.Net.BCrypt.HashPassword("your-secure-password");
        Users.Add(new User
        {
            Email = "your-admin-email@example.com",
            PasswordHash = adminPass,
            Role = "Admin",
            Name = "Admin Name"
        });
    }
    
    // Thêm admin users khác nếu cần...
    
    SaveChanges();
}
```

**Bước 3**: Restart ứng dụng → Admin user sẽ được tự động tạo

### Cách 2: Tạo Admin qua API (Sau khi đã có 1 admin)

Nếu bạn đã có ít nhất 1 admin account, có thể tạo admin mới qua API:

```bash
POST /api/admin/users
Headers: Authorization: Bearer {admin_jwt_token}
Body: {
  "email": "new-admin@example.com",
  "password": "secure-password",
  "name": "New Admin",
  "role": "Admin"
}
```

### Cách 3: Tạo Admin trực tiếp trong Database (Không khuyến nghị)

**Lưu ý**: Chỉ dùng trong trường hợp khẩn cấp.

```sql
-- Hash password trước (sử dụng BCrypt)
-- Password: "your-password"
-- Hash: $2a$11$... (generate từ code C#)

INSERT INTO Users (Email, PasswordHash, Role, Name, CreatedAt)
VALUES (
    'admin@example.com',
    '$2a$11$YOUR_BCRYPT_HASH_HERE',
    'Admin',
    'Admin Name',
    NOW()
);
```

## 🔒 Bảo mật

### ⚠️ Quan trọng: Không commit credentials vào Git

1. **File `seedadmin.txt`** đã được ignore trong `.gitignore`
2. **Không commit** email/password thực tế vào repository
3. **Sử dụng environment variables** hoặc **User Secrets** cho production

### Sử dụng User Secrets (ASP.NET Core)

```bash
# Set user secret
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "secure-password"

# Trong code, đọc từ configuration:
var adminEmail = _configuration["Admin:Email"];
var adminPassword = _configuration["Admin:Password"];
```

### Sử dụng Environment Variables

```bash
# Set environment variables
export ADMIN_EMAIL="admin@example.com"
export ADMIN_PASSWORD="secure-password"

# Hoặc trong appsettings.Production.json (không commit file này)
{
  "Admin": {
    "Email": "admin@example.com",
    "Password": "secure-password"
  }
}
```

## 📝 Best Practices

### 1. Password Requirements

- **Minimum 8 characters**
- **Kết hợp chữ hoa, chữ thường, số, ký tự đặc biệt**
- **Không dùng password mặc định** trong production

### 2. Email Requirements

- **Sử dụng email thực tế** để có thể reset password
- **Không dùng email test** trong production

### 3. Production Setup

```csharp
// Trong SeedAdminUsers(), chỉ seed trong Development
if (_environment.IsDevelopment())
{
    // Seed test admin
}
else
{
    // Seed production admin từ configuration
    var adminEmail = _configuration["Admin:Email"];
    var adminPassword = _configuration["Admin:Password"];
    // ...
}
```

## 🧪 Testing

### Test Seed Admin

1. **Xóa admin user hiện tại** (nếu có):
   ```sql
   DELETE FROM Users WHERE Role = 'Admin';
   ```

2. **Restart ứng dụng**

3. **Kiểm tra admin user đã được tạo**:
   ```sql
   SELECT * FROM Users WHERE Role = 'Admin';
   ```

4. **Test login**:
   ```bash
   POST /api/auth/login
   Body: {
     "email": "admin-email@example.com",
     "password": "admin-password"
   }
   ```

## 🔄 Reset Admin Password

### Cách 1: Qua API (Nếu đã có admin khác)

```bash
POST /api/auth/change_profile
Headers: Authorization: Bearer {admin_jwt_token}
Body: {
  "id": {admin_user_id},
  "currentPassWord": "old-password",
  "newPassWord": "new-password"
}
```

### Cách 2: Qua OTP Email

```bash
# Request OTP
POST /api/auth/send-otp
Body: "admin-email@example.com"

# Verify OTP và reset password
POST /api/auth/verify-otp
Body: {
  "email": "admin-email@example.com",
  "code": "123456",
  "newPassword": "new-password"
}
```

### Cách 3: Trực tiếp trong Database (Khẩn cấp)

```csharp
// Generate new hash trong C# code
var newHash = BCrypt.Net.BCrypt.HashPassword("new-password");

// Update trong SQL
UPDATE Users 
SET PasswordHash = '{newHash}'
WHERE Email = 'admin@example.com';
```

## 📚 Related Documentation

- [Authentication](./README_AUTHENTICATION.md) - Chi tiết về authentication
- [Database](./README_DATABASE.md) - Database schema và models
- [API Controllers](./README_API_CONTROLLERS.md) - Admin endpoints

## ⚠️ Lưu ý quan trọng

1. **Không commit** `seedadmin.txt` hoặc credentials vào Git
2. **Đổi password mặc định** ngay sau khi setup production
3. **Sử dụng strong passwords** cho admin accounts
4. **Enable 2FA** nếu có thể (future enhancement)
5. **Log admin activities** để audit (future enhancement)

---

**Last Updated**: 2024-12-17

