using EcomerceBE.Data;
using EcomerceBE.DTOs.ModelAI;
using EcomerceBE.Models.ModelAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcomerceBE.Controllers.ModelAI
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserBehaviorController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserBehaviorController> _logger;

        public UserBehaviorController(AppDbContext context, ILogger<UserBehaviorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ghi nhận hành vi người dùng (xem, thêm vào giỏ, mua, wishlist)
        /// </summary>
        /// <param name="request">Thông tin hành vi người dùng</param>
        /// <returns></returns>
        [HttpPost("track")]
        [Authorize] // Yêu cầu đăng nhập (có thể bỏ nếu muốn track cả user chưa đăng nhập)
        public async Task<IActionResult> TrackUserBehavior([FromBody] UserBehaviorRequest request)
        {
            try
            {
                // Lấy UserId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Kiểm tra sản phẩm có tồn tại không (không giới hạn IsActive để vẫn log được cho các sản phẩm flash sale / đã ngưng hiển thị)
                var productExists = await _context.Products
                    .AnyAsync(p => p.ProductId == request.ProductId);
                
                if (!productExists)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Tạo log hành vi
                var behaviorLog = new UserBehaviorLog
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    BehaviorType = request.BehaviorType,
                    ViewDuration = request.ViewDuration,
                    SessionId = request.SessionId ?? HttpContext.Session.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Metadata = request.Metadata,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserBehaviorLogs.Add(behaviorLog);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Behavior tracked successfully", 
                    behaviorLogId = behaviorLog.UserBehaviorLogId 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking user behavior");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Ghi nhận hành vi cho user chưa đăng nhập (anonymous)
        /// </summary>
        [HttpPost("track-anonymous")]
        public async Task<IActionResult> TrackAnonymousBehavior([FromBody] UserBehaviorRequest request)
        {
            try
            {
                // Kiểm tra sản phẩm có tồn tại không (không giới hạn IsActive)
                var productExists = await _context.Products
                    .AnyAsync(p => p.ProductId == request.ProductId);
                
                if (!productExists)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Tạo log hành vi (không có UserId)
                var behaviorLog = new UserBehaviorLog
                {
                    UserId = null, // Anonymous user
                    ProductId = request.ProductId,
                    BehaviorType = request.BehaviorType,
                    ViewDuration = request.ViewDuration,
                    SessionId = request.SessionId ?? HttpContext.Session.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Metadata = request.Metadata,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserBehaviorLogs.Add(behaviorLog);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Anonymous behavior tracked successfully", 
                    behaviorLogId = behaviorLog.UserBehaviorLogId 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking anonymous behavior");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy lịch sử hành vi của người dùng
        /// </summary>
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetUserBehaviorHistory(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string? behaviorType = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var query = _context.UserBehaviorLogs
                    .Where(b => b.UserId == userId)
                    .Include(b => b.Product)
                    .OrderByDescending(b => b.CreatedAt);

                if (!string.IsNullOrEmpty(behaviorType))
                {
                    query = (IOrderedQueryable<UserBehaviorLog>)query.Where(b => b.BehaviorType == behaviorType);
                }

                var totalCount = await query.CountAsync();
                var behaviors = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        b.UserBehaviorLogId,
                        b.ProductId,
                        ProductName = b.Product.Name,
                        b.BehaviorType,
                        b.ViewDuration,
                        b.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = behaviors,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user behavior history");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}

