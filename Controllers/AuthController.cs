      using BCrypt.Net;
using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth; // ✅ cần cài package Google.Apis.Auth
using EcomerceBE.DTOs;
using EcomerceBE.Service.auth;
using Microsoft.EntityFrameworkCore;
using EcomerceBE.Service;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Security.Cryptography;
using static System.Net.WebRequestMethods;
using Microsoft.Extensions.Options;
namespace Controllers.AuthController
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly string _googleClientId;
        private readonly IAuthService _authService;
        private readonly string? _config;
        private readonly AppUrls _urls;
        public AuthController(IConfiguration configuration, AppDbContext context, IAuthService authService, IOptions<AppUrls> urls)
        {
            _jwtKey = configuration["Jwt:Key"];
            _jwtIssuer = configuration["Jwt:Issuer"];
            _jwtAudience = configuration["Jwt:Audience"];
            _googleClientId = configuration["GoogleAuth:ClientId"];
            _context = context;
            _authService = authService;
            _config = configuration["Authentication:Google:ClientId"];
            _urls = urls.Value;
        }

        // ========================= ĐĂNG KÝ =========================
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel registerModel)
        {
            if (_context.Users.Any(u => u.Email == registerModel.Email))
                return BadRequest("Email đã tồn tại");

            var newUser = new User
            {
                Name = registerModel.Name,
                Email = registerModel.Email,
                Role = "User",
                PasswordHash = _authService.HashPassword(registerModel.Password),

            };

            _context.Users.Add(newUser);
            _context.SaveChanges();
            return Ok("Đăng ký thành công");
        }

        // ========================= ĐĂNG NHẬP =========================

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            // 1️⃣ Kiểm tra user có tồn tại không
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
                return Unauthorized("Sai tài khoản hoặc mật khẩu");

            bool isValid = false;

            // Nếu hash lưu trong DB có vẻ là bcrypt (thường bắt đầu bằng $2a$, $2b$, $2y$)
            if (user.PasswordHash.StartsWith("$2"))
            {
                isValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            }
            else
            {
                // fallback: verify theo kiểu cũ (hash SHA256 hoặc gì đó trong _authService)
                isValid = _authService.VerifyPassword(model.Password, user.PasswordHash);
            }

            if (!isValid)
                return Unauthorized("Sai tài khoản hoặc mật khẩu");

            // 2️⃣ Tạo access token
            var token = _authService.GenerateJwtToken(user);

            // 3️⃣ Tạo refresh token
            var refreshToken = _authService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _context.SaveChanges();

            // 4️⃣ Trả về cho frontend
            return Ok(new
            {
                token,
                refreshToken
            });
        }



        //REFRESH TOKEN
        // AllowAnonymous: This endpoint is used to refresh expired tokens, so it must be accessible without authentication
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost("refresh-token")]
        public IActionResult Refresh([FromBody] RefreshRequest model)
        {
            if (model is null)
                return BadRequest("Invalid request");

            var principal = _authService.GetPrincipalFromExpiredToken(model.Token);
            if (principal == null)
                return Unauthorized("Invalid token");

            // Try to get email from claims, fallback to user ID if email claim doesn't exist (for old tokens)
            var email = principal.FindFirstValue(ClaimTypes.Email);
            User? user = null;

            if (!string.IsNullOrEmpty(email))
            {
                user = _context.Users.FirstOrDefault(u => u.Email == email);
            }
            else
            {
                // Fallback: try to get user ID from claims (for backward compatibility with old tokens)
                var userIdClaim = principal.FindFirst("id")?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    user = _context.Users.FirstOrDefault(u => u.Id == userId);
                }
            }

            if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized("Invalid refresh token");

            var newToken = _authService.GenerateJwtToken(user);
            var newRefresh = _authService.GenerateRefreshToken();

            user.RefreshToken = newRefresh;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _context.SaveChanges();

            return Ok(new
            {
                token = newToken,
                refreshToken = newRefresh
            });
        }

        //GOOGLE LOGIN
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest model)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(model.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config }
                });
            }
            catch
            {
                return Unauthorized("Token Google không hợp lệ");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == payload.Email);
            if (user == null)
            {
                user = new User
                {
                    Name = payload.Name,
                    Email = payload.Email,
                    Role = "User",
                    PasswordHash = _authService.HashPassword(Guid.NewGuid().ToString()) // random password
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            var token = _authService.GenerateJwtToken(user);
            var refreshToken = _authService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _context.SaveChanges();

            return Ok(new
            {
                token,
                refreshToken
            });
        }



        //  Send OTP
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return NotFound("Email không tồn tại trong hệ thống");

            // Sinh OTP 6 chữ số
            var otp = new Random().Next(100000, 999999).ToString();

            // Lưu OTP vào DB (ghi đè nếu có)
            var oldOtp = _context.OtpCodes.FirstOrDefault(o => o.Email == email);
            if (oldOtp != null)
                _context.OtpCodes.Remove(oldOtp);

            _context.OtpCodes.Add(new OtpCode
            {
                Email = email,
                Code = otp,
                Expiration = DateTime.UtcNow.AddMinutes(5)
            });
            _context.SaveChanges();

            // Build base URL for logo (backend domain)
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var logoUrl = $"{baseUrl}/assets/logo/logo-email.png";

            // Send email with styled template
            var emailService = new Email(HttpContext.RequestServices.GetRequiredService<IConfiguration>());
            var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>OTP Verification</title>
  <style>
    body {{ margin:0; padding:0; background:#f4f4f4; font-family:'Helvetica Neue',Helvetica,Arial,sans-serif; }}
    table {{ border-collapse:collapse; width:100%; }}
    .container {{ max-width:600px; margin:0 auto; background:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 10px rgba(0,0,0,0.1); }}
    .header {{ background:#ffffff; padding:30px; text-align:center; border-bottom:2px solid #f0f0f0; }}
    .content {{ padding:40px 30px; text-align:center; color:#333; }}
    .otp-code {{ font-size:32px; font-weight:bold; letter-spacing:5px; color:#DB4444; margin:20px 0; background:#fdf2f2; padding:15px; border-radius:5px; display:inline-block; border:1px dashed #DB4444; }}
    .footer {{ background:#000; color:#fff; padding:30px; text-align:center; font-size:14px; line-height:1.6; }}
    .footer a {{ color:#fff; text-decoration:none; }}
    .note {{ font-size:13px; color:#666; margin-top:20px; font-style:italic; }}
  </style>
</head>
<body>
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
    <tr><td style=""padding:20px 0;"">
      <div class=""container"">
        <div class=""header"">
          <img src=""{logoUrl}"" alt=""EXCLUSIVE"" style=""max-width:150px;height:auto;"">
        </div>
        <div class=""content"">
          <h2 style=""margin-top:0;"">Verification Code</h2>
          <p>Hello,</p>
          <p>You recently requested to reset your password (or sign in) for your <strong>EXCLUSIVE</strong> account.</p>
          <p>Here is your One-Time Password (OTP):</p>
          <div class=""otp-code"">{otp}</div>
          <p>This code is valid for <strong>5 minutes</strong>.</p>
          <div class=""note"">
            *If you did not request this code, please ignore this email or contact our support team immediately.
          </div>
        </div>
        <div class=""footer"">
          <h3 style=""margin-top:0;font-size:18px;"">Customer Support</h3>
          <p>
            <strong>Address:</strong> 111 Bijoy sarani, Dhaka, DH 1515, Bangladesh.<br>
            <strong>Email:</strong> <a href=""mailto:exclusive@gmail.com"">exclusive@gmail.com</a><br>
            <strong>Phone:</strong> +88015-88888-9999
          </p>
          <p style=""margin-top:20px;font-size:12px;color:#aaaaaa;"">&copy; 2025 Exclusive. All rights reserved.</p>
        </div>
      </div>
    </td></tr>
  </table>
</body>
</html>";
            await emailService.SendEmailAsync(email, "Mã xác thực OTP của bạn", html);

            return Ok("Đã gửi OTP qua email");
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtpAndResetPassword([FromBody] OtpVerifyRequest model)
        {
            // 1️⃣ Kiểm tra mã OTP
            var otpRecord = _context.OtpCodes.FirstOrDefault(o => o.Email == model.Email && o.Code == model.Code);

            if (otpRecord == null)
                return BadRequest("Mã OTP không hợp lệ");

            if (otpRecord.Expiration < DateTime.UtcNow)
                return BadRequest("Mã OTP đã hết hạn");

            // 2️⃣ Tìm user tương ứng với email
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
                return NotFound("Không tìm thấy tài khoản");

            // 3️⃣ Cập nhật mật khẩu mới (nhớ mã hóa nếu có)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            // 4️⃣ Xóa OTP sau khi dùng
            _context.OtpCodes.Remove(otpRecord);

            // 5️⃣ Lưu thay đổi
            _context.SaveChanges();

            return Ok("Đổi mật khẩu thành công");
        }


        //send email verification

        [HttpPost("sendEmailVerify")]
        public async Task<IActionResult> Send([FromBody] string email)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == email);
            // Để tránh lộ tài khoản, vẫn tạo link chỉ khi user tồn tại; nếu không tồn tại có thể trả message chung
            if (user == null)
                return Ok(new { message = "Nếu email tồn tại, hệ thống đã gửi hướng dẫn xác nhận." });

            if (user.status=="Active")
                return Ok(new { message = "Email đã được xác minh." });

            // Phát token nếu chưa có hoặc đã hết hạn
            if (string.IsNullOrEmpty(user.EmailVerificationToken) || user.EmailVerificationExpiry is null || user.EmailVerificationExpiry < DateTime.UtcNow)
            {
                user.EmailVerificationToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                user.EmailVerificationExpiry = DateTime.UtcNow.AddDays(2);
                await _context.SaveChangesAsync();
            }

            var link = $"{_urls.FrontendBaseUrl}/verify-email?email={WebUtility.UrlEncode(user.Email)}&token={WebUtility.UrlEncode(user.EmailVerificationToken)}";

            var html = $@"<p>Nhấn vào liên kết sau để xác nhận email:</p>
                      <p><a href=""{link}"">Xác nhận email</a></p>";

            var emailService = new Email(HttpContext.RequestServices.GetRequiredService<IConfiguration>());
            await emailService.SendEmailAsync(user.Email, "Xác nhận email", html);


            return Ok(new
            {
                message = "Nếu email tồn tại, hệ thống đã gửi hướng dẫn xác nhận.",
                verificationLink = link
            });
        }



        // submit email verification
        [HttpPost("verification/confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmEmailRequest req)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == req.Email);
            if (user == null) return BadRequest(new { message = "Email không hợp lệ." });
            if (user.status=="Active") return Ok(new { message = "Email đã xác minh." });
            if (string.IsNullOrEmpty(user.EmailVerificationToken) || user.EmailVerificationExpiry is null)
                return BadRequest(new { message = "Không tìm thấy token." });
            if (user.EmailVerificationExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Token đã hết hạn." });
            if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(user.EmailVerificationToken),
                System.Text.Encoding.UTF8.GetBytes(req.Token)))
                return BadRequest(new { message = "Token không hợp lệ." });

            user.status ="Active";
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xác nhận email thành công." });
        }

        [HttpPost("change_profile")]
        public IActionResult ChangeProfile([FromBody] ChangeProfile profile)
        {
            // Tìm user theo Id
            var user = _context.Users.Find(profile.Id);
            if (user == null)
                return BadRequest(new { message = "User not found" });

            // Nếu có nhập NewPassWord thì verify CurrentPassWord
            if (!string.IsNullOrEmpty(profile.NewPassWord))
            {
                if (string.IsNullOrEmpty(profile.CurrentPassWord))
                {
                    return BadRequest(new { message = "Current password is required to set a new password" });
                }

                bool isValid = _authService.VerifyPassword(profile.CurrentPassWord, user.PasswordHash);
                if (!isValid)
                    return Unauthorized(new { message = "Current password is incorrect" });

                user.PasswordHash = _authService.HashPassword(profile.NewPassWord);
            }

            // Cập nhật Name nếu có Fname hoặc Lname
            if (!string.IsNullOrEmpty(profile.Fname) || !string.IsNullOrEmpty(profile.Lname))
            {
                user.Name = $"{profile.Fname ?? ""} {profile.Lname ?? ""}".Trim();
            }

            // Cập nhật avatar nếu có
            if (!string.IsNullOrEmpty(profile.Avatar))
            {
                user.Avatar = profile.Avatar;
            }

            // Cập nhật address nếu FE gửi lên
            if (profile.Address != null)
            {
                user.Addresses = new List<Address> { profile.Address };
            }

            _context.SaveChanges();

            return Ok(new { message = "Profile updated successfully" });
        }


    }
}
