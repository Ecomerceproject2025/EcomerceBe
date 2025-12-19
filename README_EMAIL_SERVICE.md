# Email Service

## 📋 Tổng quan

Email Service trong EcomerceBE sử dụng **MailKit** để gửi email qua SMTP. Hệ thống hỗ trợ gửi email HTML với templates đẹp mắt cho các mục đích như OTP verification, email verification, và notifications.

## 📁 Files liên quan

### Service

- **`Service/Email.cs`**
  - `SendEmailAsync(string toEmail, string subject, string body)` - Gửi email

### Usage

Email service được sử dụng trong:
- **`Controllers/AuthController.cs`** - OTP, email verification
- Các controllers khác cần gửi email

## ⚙️ Cấu hình

### appsettings.json

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

### Gmail App Password Setup

1. Truy cập [Google Account Settings](https://myaccount.google.com/)
2. Enable **2-Step Verification**
3. Tạo **App Password**:
   - Go to Security → 2-Step Verification → App passwords
   - Select "Mail" và "Other (Custom name)"
   - Copy app password (16 characters)

4. Sử dụng app password trong `appsettings.json`:
```json
{
  "EmailSettings": {
    "Password": "abcd efgh ijkl mnop"
  }
}
```

### Other SMTP Providers

#### Outlook/Hotmail

```json
{
  "EmailSettings": {
    "Host": "smtp-mail.outlook.com",
    "Port": 587
  }
}
```

#### SendGrid

```json
{
  "EmailSettings": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "From": "apikey",
    "Password": "your-sendgrid-api-key"
  }
}
```

## 📝 Implementation

### Email Service

```csharp
public class Email
{
    private readonly IConfiguration _config;

    public Email(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var emailSettings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Ecomerce App", emailSettings["From"]));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(emailSettings["Host"], int.Parse(emailSettings["Port"]), false);
        await client.AuthenticateAsync(emailSettings["From"], emailSettings["Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
```

### Usage trong Controllers

```csharp
[HttpPost("send-otp")]
public async Task<IActionResult> SendOtp([FromBody] string email)
{
    // Generate OTP
    var otp = new Random().Next(100000, 999999).ToString();
    
    // Build HTML email template
    var html = $@"
<!DOCTYPE html>
<html>
<head>
  <style>
    body {{ font-family: Arial, sans-serif; }}
    .otp-code {{ font-size: 32px; font-weight: bold; color: #DB4444; }}
  </style>
</head>
<body>
  <h2>Your OTP Code</h2>
  <div class=""otp-code"">{otp}</div>
  <p>This code is valid for 5 minutes.</p>
</body>
</html>";

    // Send email
    var emailService = new Email(HttpContext.RequestServices.GetRequiredService<IConfiguration>());
    await emailService.SendEmailAsync(email, "OTP Verification", html);
    
    return Ok("OTP sent successfully");
}
```

## 🎨 Email Templates

### OTP Email Template

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <style>
    body { margin:0; padding:0; background:#f4f4f4; font-family:'Helvetica Neue',Helvetica,Arial,sans-serif; }
    .container { max-width:600px; margin:0 auto; background:#ffffff; border-radius:8px; }
    .header { background:#ffffff; padding:30px; text-align:center; }
    .content { padding:40px 30px; text-align:center; }
    .otp-code { font-size:32px; font-weight:bold; letter-spacing:5px; color:#DB4444; }
    .footer { background:#000; color:#fff; padding:30px; text-align:center; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <img src="{logoUrl}" alt="EXCLUSIVE" style="max-width:150px;">
    </div>
    <div class="content">
      <h2>Verification Code</h2>
      <p>Your OTP code is:</p>
      <div class="otp-code">{otp}</div>
      <p>This code is valid for 5 minutes.</p>
    </div>
    <div class="footer">
      <p>&copy; 2025 Exclusive. All rights reserved.</p>
    </div>
  </div>
</body>
</html>
```

### Email Verification Template

```html
<!DOCTYPE html>
<html>
<head>
  <style>
    body { font-family: Arial, sans-serif; }
    .button { background-color: #DB4444; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }
  </style>
</head>
<body>
  <h2>Verify Your Email</h2>
  <p>Click the button below to verify your email address:</p>
  <a href="{verificationLink}" class="button">Verify Email</a>
  <p>Or copy this link: {verificationLink}</p>
</body>
</html>
```

## 🔍 Logo trong Email

Email templates có thể include logo từ backend:

```csharp
var baseUrl = $"{Request.Scheme}://{Request.Host}";
var logoUrl = $"{baseUrl}/assets/logo/logo-email.png";

var html = $@"
<div class=""header"">
  <img src=""{logoUrl}"" alt=""Logo"" style=""max-width:150px;"">
</div>";
```

**Lưu ý**: Logo phải được serve từ `wwwroot/assets/logo/logo-email.png` và `app.UseStaticFiles()` đã được enable trong `Program.cs`.

## 🐛 Troubleshooting

### Lỗi: "Authentication failed"

- Kiểm tra `EmailSettings:Password` đúng (app password, không phải account password)
- Đảm bảo 2-Step Verification đã enable
- Kiểm tra `EmailSettings:From` đúng email address

### Lỗi: "Connection timeout"

- Kiểm tra `EmailSettings:Host` và `Port` đúng
- Kiểm tra firewall/network không block SMTP port
- Thử dùng port 465 với SSL thay vì 587

### Lỗi: "Logo not displaying"

- Kiểm tra logo file tồn tại trong `wwwroot/assets/logo/`
- Kiểm tra `app.UseStaticFiles()` đã được gọi trong `Program.cs`
- Kiểm tra URL logo đúng format

### Email không được gửi

- Kiểm tra SMTP settings đúng
- Kiểm tra logs để xem có errors không
- Thử test với Gmail SMTP trước

## 📚 Related Documentation

- [Authentication](./README_AUTHENTICATION.md) - OTP và email verification usage
- [Services](./README_SERVICES.md) - Email service details

---

**Last Updated**: 2024-12-17

