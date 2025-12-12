using System.ComponentModel.DataAnnotations;
using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/coupons")]
    [Authorize(Roles = "Admin")]
    public class CouponController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CouponController> _logger;

        public CouponController(AppDbContext context, ILogger<CouponController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/coupons
        [HttpGet]
        public async Task<IActionResult> GetCoupons(
            [FromQuery] string? type = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Coupons.AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(c => c.Type != null && c.Type.ToLower() == type.ToLower());
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var coupons = await query
                .OrderByDescending(c => c.CouponId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.CouponId,
                    c.Code,
                    c.Description,
                    c.Type,
                    c.DiscountValue,
                    c.IsPercent,
                    c.StartDate,
                    c.EndDate,
                    c.UsageCount,
                    c.MaxUsage,
                    c.IsActive,
                    c.MinOrderAmount,
                    c.MaxDiscountAmount,
                    ProductCount = c.ProductCoupons != null ? c.ProductCoupons.Count : 0,
                    UserCount = c.UserCoupons != null ? c.UserCoupons.Count : 0,
                    ShippingMethodCount = c.ShippingMethodCoupons != null ? c.ShippingMethodCoupons.Count : 0
                })
                .ToListAsync();

            return Ok(new
            {
                data = coupons,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }

        // GET /api/coupons/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCouponById(int id)
        {
            var coupon = await _context.Coupons
                .Include(c => c.ProductCoupons)
                    .ThenInclude(pc => pc.Product)
                .Include(c => c.UserCoupons)
                    .ThenInclude(uc => uc.User)
                .Include(c => c.ShippingMethodCoupons)
                    .ThenInclude(smc => smc.ShippingMethod)
                .FirstOrDefaultAsync(c => c.CouponId == id);

            if (coupon == null)
                return NotFound(new { message = "Coupon not found." });

            var productsList = new List<object>();
            if (coupon.ProductCoupons != null)
            {
                productsList = coupon.ProductCoupons.Select(pc => new
                {
                    pc.ProductId,
                    productName = pc.Product.Name
                }).Cast<object>().ToList();
            }

            var usersList = new List<object>();
            if (coupon.UserCoupons != null)
            {
                usersList = coupon.UserCoupons.Select(uc => new
                {
                    uc.UserId,
                    userName = uc.User.Name,
                    userEmail = uc.User.Email,
                    uc.UsedCount,
                    uc.MaxUsagePerUser
                }).Cast<object>().ToList();
            }

            var shippingMethodsList = new List<object>();
            if (coupon.ShippingMethodCoupons != null)
            {
                shippingMethodsList = coupon.ShippingMethodCoupons.Select(smc => new
                {
                    smc.ShippingMethodId,
                    shippingMethodName = smc.ShippingMethod.Name
                }).Cast<object>().ToList();
            }

            return Ok(new
            {
                couponId = coupon.CouponId,
                code = coupon.Code,
                description = coupon.Description,
                type = coupon.Type,
                discountValue = coupon.DiscountValue,
                isPercent = coupon.IsPercent,
                startDate = coupon.StartDate,
                endDate = coupon.EndDate,
                usageCount = coupon.UsageCount,
                maxUsage = coupon.MaxUsage,
                isActive = coupon.IsActive,
                minOrderAmount = coupon.MinOrderAmount,
                maxDiscountAmount = coupon.MaxDiscountAmount,
                products = productsList,
                users = usersList,
                shippingMethods = shippingMethodsList
            });
        }

        // POST /api/coupons
        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Code is required." });

            // Check if code already exists
            var exists = await _context.Coupons.AnyAsync(c => c.Code == dto.Code);
            if (exists)
                return BadRequest(new { message = "Coupon code already exists." });

            var coupon = new Coupon
            {
                Code = dto.Code.Trim().ToUpper(),
                Description = dto.Description?.Trim(),
                Type = dto.Type ?? "Product",
                DiscountValue = dto.DiscountValue,
                IsPercent = dto.IsPercent,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MaxUsage = dto.MaxUsage,
                IsActive = dto.IsActive ?? true,
                MinOrderAmount = dto.MinOrderAmount,
                MaxDiscountAmount = dto.MaxDiscountAmount
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            // Add product associations if type is Product
            if (coupon.Type == "Product" && dto.ProductIds != null && dto.ProductIds.Any())
            {
                foreach (var productId in dto.ProductIds)
                {
                    _context.ProductCoupons.Add(new ProductCoupon
                    {
                        CouponId = coupon.CouponId,
                        ProductId = productId
                    });
                }
            }

            // Add user associations if type is User
            if (coupon.Type == "User" && dto.UserIds != null && dto.UserIds.Any())
            {
                foreach (var userId in dto.UserIds)
                {
                    _context.UserCoupons.Add(new UserCoupon
                    {
                        CouponId = coupon.CouponId,
                        UserId = userId,
                        MaxUsagePerUser = dto.MaxUsagePerUser
                    });
                }
            }

            // Add shipping method associations if type is ShippingMethod
            if (coupon.Type == "ShippingMethod" && dto.ShippingMethodIds != null && dto.ShippingMethodIds.Any())
            {
                foreach (var shippingMethodId in dto.ShippingMethodIds)
                {
                    _context.ShippingMethodCoupons.Add(new ShippingMethodCoupon
                    {
                        CouponId = coupon.CouponId,
                        ShippingMethodId = shippingMethodId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Coupon created successfully.",
                couponId = coupon.CouponId,
                code = coupon.Code
            });
        }

        // PATCH /api/coupons/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] UpdateCouponDto dto)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponId == id);

            if (coupon == null)
                return NotFound(new { message = "Coupon not found." });

            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var newCode = dto.Code.Trim().ToUpper();
                if (newCode != coupon.Code)
                {
                    var exists = await _context.Coupons.AnyAsync(c => c.Code == newCode && c.CouponId != id);
                    if (exists)
                        return BadRequest(new { message = "Coupon code already exists." });
                    coupon.Code = newCode;
                    hasChanges = true;
                }
            }

            if (dto.Description != null)
            {
                coupon.Description = dto.Description.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Type))
            {
                coupon.Type = dto.Type;
                hasChanges = true;
            }

            if (dto.DiscountValue.HasValue)
            {
                coupon.DiscountValue = dto.DiscountValue.Value;
                hasChanges = true;
            }

            if (dto.IsPercent.HasValue)
            {
                coupon.IsPercent = dto.IsPercent.Value;
                hasChanges = true;
            }

            if (dto.StartDate.HasValue)
            {
                coupon.StartDate = dto.StartDate;
                hasChanges = true;
            }

            if (dto.EndDate.HasValue)
            {
                coupon.EndDate = dto.EndDate;
                hasChanges = true;
            }

            if (dto.MaxUsage.HasValue)
            {
                coupon.MaxUsage = dto.MaxUsage.Value;
                hasChanges = true;
            }

            if (dto.IsActive.HasValue)
            {
                coupon.IsActive = dto.IsActive.Value;
                hasChanges = true;
            }

            if (dto.MinOrderAmount.HasValue)
            {
                coupon.MinOrderAmount = dto.MinOrderAmount;
                hasChanges = true;
            }

            if (dto.MaxDiscountAmount.HasValue)
            {
                coupon.MaxDiscountAmount = dto.MaxDiscountAmount;
                hasChanges = true;
            }

            if (!hasChanges)
                return Ok(new { message = "No changes detected.", couponId = coupon.CouponId });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Coupon updated successfully.",
                couponId = coupon.CouponId
            });
        }

        // POST /api/coupons/{id}/products
        [HttpPost("{id}/products")]
        public async Task<IActionResult> AddProductsToCoupon(int id, [FromBody] List<int> productIds)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
                return NotFound(new { message = "Coupon not found." });

            if (coupon.Type != "Product")
                return BadRequest(new { message = "This coupon is not a product coupon." });

            foreach (var productId in productIds)
            {
                var exists = await _context.ProductCoupons
                    .AnyAsync(pc => pc.CouponId == id && pc.ProductId == productId);
                
                if (!exists)
                {
                    _context.ProductCoupons.Add(new ProductCoupon
                    {
                        CouponId = id,
                        ProductId = productId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Products added to coupon successfully." });
        }

        // DELETE /api/coupons/{id}/products/{productId}
        [HttpDelete("{id}/products/{productId}")]
        public async Task<IActionResult> RemoveProductFromCoupon(int id, int productId)
        {
            var productCoupon = await _context.ProductCoupons
                .FirstOrDefaultAsync(pc => pc.CouponId == id && pc.ProductId == productId);

            if (productCoupon == null)
                return NotFound(new { message = "Product not found in coupon." });

            _context.ProductCoupons.Remove(productCoupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product removed from coupon successfully." });
        }

        // POST /api/coupons/{id}/users
        [HttpPost("{id}/users")]
        public async Task<IActionResult> AddUsersToCoupon(int id, [FromBody] AddUsersToCouponDto dto)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
                return NotFound(new { message = "Coupon not found." });

            if (coupon.Type != "User")
                return BadRequest(new { message = "This coupon is not a user coupon." });

            foreach (var userId in dto.UserIds)
            {
                var exists = await _context.UserCoupons
                    .AnyAsync(uc => uc.CouponId == id && uc.UserId == userId);
                
                if (!exists)
                {
                    _context.UserCoupons.Add(new UserCoupon
                    {
                        CouponId = id,
                        UserId = userId,
                        MaxUsagePerUser = dto.MaxUsagePerUser
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Users added to coupon successfully." });
        }

        // DELETE /api/coupons/{id}/users/{userId}
        [HttpDelete("{id}/users/{userId}")]
        public async Task<IActionResult> RemoveUserFromCoupon(int id, int userId)
        {
            var userCoupon = await _context.UserCoupons
                .FirstOrDefaultAsync(uc => uc.CouponId == id && uc.UserId == userId);

            if (userCoupon == null)
                return NotFound(new { message = "User not found in coupon." });

            _context.UserCoupons.Remove(userCoupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User removed from coupon successfully." });
        }

        // POST /api/coupons/{id}/shipping-methods
        [HttpPost("{id}/shipping-methods")]
        public async Task<IActionResult> AddShippingMethodsToCoupon(int id, [FromBody] List<int> shippingMethodIds)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
                return NotFound(new { message = "Coupon not found." });

            if (coupon.Type != "ShippingMethod")
                return BadRequest(new { message = "This coupon is not a shipping method coupon." });

            foreach (var shippingMethodId in shippingMethodIds)
            {
                var exists = await _context.ShippingMethodCoupons
                    .AnyAsync(smc => smc.CouponId == id && smc.ShippingMethodId == shippingMethodId);
                
                if (!exists)
                {
                    _context.ShippingMethodCoupons.Add(new ShippingMethodCoupon
                    {
                        CouponId = id,
                        ShippingMethodId = shippingMethodId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Shipping methods added to coupon successfully." });
        }

        // DELETE /api/coupons/{id}/shipping-methods/{shippingMethodId}
        [HttpDelete("{id}/shipping-methods/{shippingMethodId}")]
        public async Task<IActionResult> RemoveShippingMethodFromCoupon(int id, int shippingMethodId)
        {
            var shippingMethodCoupon = await _context.ShippingMethodCoupons
                .FirstOrDefaultAsync(smc => smc.CouponId == id && smc.ShippingMethodId == shippingMethodId);

            if (shippingMethodCoupon == null)
                return NotFound(new { message = "Shipping method not found in coupon." });

            _context.ShippingMethodCoupons.Remove(shippingMethodCoupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipping method removed from coupon successfully." });
        }

        // DELETE /api/coupons/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponId == id);

            if (coupon == null)
                return NotFound(new { message = "Coupon not found." });

            // Check if coupon has been used
            if (coupon.UsageCount > 0)
            {
                // Soft delete
                coupon.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Coupon deactivated (has been used)." });
            }

            // Hard delete if not used
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Coupon deleted successfully." });
        }

        // DTOs
        public class CreateCouponDto
        {
            [Required] public string Code { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? Type { get; set; } // "Product", "User", "ShippingMethod"
            [Required] public decimal DiscountValue { get; set; }
            public bool IsPercent { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int MaxUsage { get; set; } = 100;
            public bool? IsActive { get; set; }
            public decimal? MinOrderAmount { get; set; }
            public int? MaxDiscountAmount { get; set; }
            public List<int>? ProductIds { get; set; }
            public List<int>? UserIds { get; set; }
            public List<int>? ShippingMethodIds { get; set; }
            public int? MaxUsagePerUser { get; set; }
        }

        public class UpdateCouponDto
        {
            public string? Code { get; set; }
            public string? Description { get; set; }
            public string? Type { get; set; }
            public decimal? DiscountValue { get; set; }
            public bool? IsPercent { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? MaxUsage { get; set; }
            public bool? IsActive { get; set; }
            public decimal? MinOrderAmount { get; set; }
            public int? MaxDiscountAmount { get; set; }
        }

        public class AddUsersToCouponDto
        {
            [Required] public List<int> UserIds { get; set; } = new();
            public int? MaxUsagePerUser { get; set; }
        }
    }
}

