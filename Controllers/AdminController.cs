using EcomerceBE.Data;
using EcomerceBE.DTOs;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using static EcomerceBE.Controllers.ProductViewController;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Chỉ admin mới được vào controller này
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public class AdminCreateUserDto
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? Role { get; set; } = "User";
            public string? Status { get; set; } = "active";
            public string? Password { get; set; }
        }

        public class AdminUpdateUserDto
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Role { get; set; }
            public string? Status { get; set; }
            public string? Password { get; set; }
        }

        // GET: api/admin/dashboard/user?page=1&limit=10
        [HttpGet("dashboard/user")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var totalUsers = await _context.Users.CountAsync();

            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.Role,
                    status = u.status,
                    u.CreatedAt,
                    TotalOrders = _context.Orders.Count(o => o.UserId == u.Id),
                    LastOrderAt = _context.Orders
                        .Where(o => o.UserId == u.Id)
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => (DateTime?)o.CreatedAt)
                        .FirstOrDefault()
                })
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new
            {
                total = totalUsers,
                page,
                limit,
                totalPages = (int)Math.Ceiling(totalUsers / (double)limit),
                data = users
            });
        }



        [HttpPost("user_filter")]
        public IActionResult FilterUsers([FromBody] UserFilter filter)
        {
            // Đảm bảo không null
            var query = _context.Users.AsQueryable();

            // Lọc dữ liệu
            if (!string.IsNullOrWhiteSpace(filter.EmailOrName))
            {
                var term = filter.EmailOrName.Trim();
                // escape ký tự wildcard nếu cần
                var escaped = term.Replace("%", "[%]").Replace("_", "[_]");
                var pattern = $"%{escaped}%";

                query = query.Where(u =>
                    EF.Functions.Like(u.Name, pattern) ||
                    EF.Functions.Like(u.Email, pattern));
            }


            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(u => u.status == filter.Status);
            }

            if (!string.IsNullOrEmpty(filter.Role))
            {
                query = query.Where(u => u.Role == filter.Role);
            }

            // Lọc theo ngày (nếu có)
            if (filter.DateCreate.HasValue)
            {
                var date = filter.DateCreate.Value.Date;
                query = query.Where(u => u.CreatedAt.Date == date);
            }

            // Lọc user có order gần đây
            if (filter.HasRecentOrders == true)
            {
                var days = filter.RecentOrdersDays ?? 30;
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var userIdsWithRecentOrders = _context.Orders
                    .Where(o => o.CreatedAt >= cutoffDate)
                    .Select(o => o.UserId)
                    .Distinct();
                query = query.Where(u => userIdsWithRecentOrders.Contains(u.Id));
            }

            // Tổng số user sau filter
            var total = query.Count();

            // Phân trang
            int page = filter.Page > 0 ? filter.Page : 1;
            int limit = filter.Limit > 0 ? filter.Limit : 10;
            var totalPages = (int)Math.Ceiling(total / (double)limit);

            var data = query
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.status,
                    u.CreatedAt,
                    TotalOrders = _context.Orders.Count(o => o.UserId == u.Id),
                    LastOrderAt = _context.Orders
                        .Where(o => o.UserId == u.Id)
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => (DateTime?)o.CreatedAt)
                        .FirstOrDefault()
                })
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return Ok(new
            {
                success = true,
                total = total,
                page,
                limit,
                totalPages = totalPages,
                data
            });
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name and Email are required." });

            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists) return BadRequest(new { message = "Email already exists." });

            var password = string.IsNullOrWhiteSpace(dto.Password) ? "Temp@" + Guid.NewGuid().ToString("N")[..6] : dto.Password;
            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role,
                status = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User created", userId = user.Id, tempPassword = password });
        }

        [HttpPatch("users/{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
                if (exists) return BadRequest(new { message = "Email already exists." });
                user.Email = dto.Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Role)) user.Role = dto.Role;
            if (!string.IsNullOrWhiteSpace(dto.Status)) user.status = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "User updated" });
        }

        [HttpPatch("users/{id:int}/ban")]
        public async Task<IActionResult> BanUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });
            user.status = "banned";
            await _context.SaveChangesAsync();
            return Ok(new { message = "User banned" });
        }

        [HttpPatch("users/{id:int}/unban")]
        public async Task<IActionResult> UnbanUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });
            user.status = "active";
            await _context.SaveChangesAsync();
            return Ok(new { message = "User unbanned" });
        }

        [HttpPatch("users/{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });
            var tempPassword = "Temp@" + Guid.NewGuid().ToString("N")[..6];
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password reset", tempPassword });
        }

        [HttpGet("recent-buyers")]
        public async Task<IActionResult> GetRecentBuyers([FromQuery] int limit = 10)
        {
            if (limit <= 0) limit = 10;
            var buyers = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Name = g.First().User.Name,
                    Email = g.First().User.Email,
                    LastOrderAt = g.Max(o => o.CreatedAt),
                    LastOrderId = g.OrderByDescending(o => o.CreatedAt).Select(o => o.OrderId).FirstOrDefault(),
                    TotalOrders = g.Count(),
                    LastOrderStatus = g.OrderByDescending(o => o.CreatedAt).Select(o => o.OrderStatus).FirstOrDefault(),
                    LastPaymentStatus = g.OrderByDescending(o => o.CreatedAt).Select(o => o.PaymentStatus).FirstOrDefault()
                })
                .OrderByDescending(x => x.LastOrderAt)
                .Take(limit)
                .ToListAsync();

            return Ok(buyers);
        }

        [HttpGet("order-alerts")]
        public async Task<IActionResult> GetOrderAlerts([FromQuery] int take = 20, [FromQuery] DateTime? since = null)
        {
            if (take <= 0) take = 20;
            var query = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (since.HasValue)
            {
                // Assume client passes UTC; compare strictly greater than last seen
                query = query.Where(o => o.CreatedAt > since.Value);
            }

            var alerts = await query
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .Select(o => new
                {
                    o.OrderId,
                    o.CreatedAt,
                    o.OrderStatus,
                    o.PaymentStatus,
                    UserId = o.UserId,
                    UserName = o.User.Name,
                    UserEmail = o.User.Email,
                    Type = o.OrderStatus != null && o.OrderStatus.ToLower().Contains("cancel") ? "CancelOrder" : "NewOrder"
                })
                .ToListAsync();
            return Ok(alerts);
        }


        [HttpGet("normal_products")]
        public async Task<IActionResult> GetNomalProduct(
                   [FromQuery] int page = 1,
                   [FromQuery] int limit = 10)
        {


            int totalProducts = await _context.Products.CountAsync();
            int totalPages = (int)Math.Ceiling(totalProducts / (double)limit);
            var products = await _context.Products
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new
                {
                    id = p.ProductId,
                    Title = p.Name,
                    p.Category.CategoryId,
                    categoryName = p.Category.Name,
                    p.Price,
                    Maxquantity = p.StockQuantity,
                    p.Description,
                    p.ImportPrice,
                    p.ReturnDeliveryDay,
                    p.IsActive,
                    ProductCoupon = p.ProductCoupons.Select(pc => pc.Coupon.Code).ToList(),
                    Category = p.Category.CategorySizes.Select(cs => cs.Size.Name).ToList(),
                    Color = p.ProductSizes
                        .SelectMany(ps => ps.ProductColors)
                        .Select(pc => pc.ColorCode)
                        .Distinct()
                        .ToList(),

                    productType= p.productType,
                    salePrice = p.FlashSaleItems
    .Where(ps => p.ProductId == ps.ProductId) // Lọc trước
    .Select(ps => ps.DiscountPrice)    // Chọn giá sau
    .FirstOrDefault(),

    saleQuantity = p.FlashSaleItems.Where(ps => p.ProductId == ps.ProductId).Select(ps => ps.saleQuantity).FirstOrDefault(),

                    // Hero: ảnh đầu tiên (nếu có)
                    heroImage = p.Images
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),
                    // Các ảnh còn lại
                    ProductImage = p.Images
                        .Skip(1)
                        .Select(img => img.ImageUrl)
                        .ToList(),

                    Sizes = p.ProductSizes
                        .Select(ps => new SizeWithColorsDto
                        {
                            SizeName = ps.Size != null
                                ? ps.Size.Name
                                : (ps.CustomValue ?? string.Empty),

                            Color = ps.ProductColors != null
                                ? ps.ProductColors.Select(pc => new ColorAndquantity
                                {
                                    ColorCode = pc.ColorCode,
                                    quantity = pc.Quantity
                                }).ToList()
                                : new List<ColorAndquantity>()
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(new {
                totalPages = totalPages,
                totalProducts = totalProducts,
                products = products,
                page = page,
            }
            );
        }






        [HttpDelete("delete_product/{id:int}")]
        public async Task<IActionResult> DeleteProduct([FromBody] int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = "Product not found" });
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product deleted successfully" });
        }

        public class UpdateVisibilityDto
        {
            public bool IsActive { get; set; }
        }

        // Toggle publish/unview normal product
        [HttpPatch("normal_products/{id:int}/visibility")]
        public async Task<IActionResult> UpdateProductVisibility(int id, [FromBody] UpdateVisibilityDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            product.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product visibility updated", IsActive = product.IsActive });
        }
    



    [HttpGet("couponsList")]
        public async Task<IActionResult> GetCouponsList()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return Ok(coupons);
        }
    }


    }


