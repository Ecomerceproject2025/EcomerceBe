using EcomerceBE.Data;
using EcomerceBE.DTOs;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: api/admin/dashboard/user?page=1&limit=10
        [HttpGet("dashboard/user")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var totalUsers = await _context.Users.CountAsync();

            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)  // sắp xếp theo ngày tạo nếu có
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.Role,
                    u.CreatedAt
                })
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


            // Tổng số user sau filter
            var total = query.Count();

            // Phân trang
            int page = filter.Page > 0 ? filter.Page : 1;
            int limit = filter.Limit > 0 ? filter.Limit : 10;
            var totalPages = (int)Math.Ceiling(total / (double)limit);

            var data = query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.status,

                    u.CreatedAt
                })
                .ToList();

            return Ok(new
            {
                Success = true,
                Total = total,
                Page = page,
                Limit = limit,
                TotalPages = totalPages,
                Data = data
            });
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
                    ProductCoupon = p.ProductCoupons.Select(pc => pc.Coupon.Code).ToList(),
                    Category = p.Category.CategorySizes.Select(cs => cs.Size.Name).ToList(),
                    Color = p.ProductSizes
                        .SelectMany(ps => ps.ProductColors)
                        .Select(pc => pc.ColorCode)
                        .Distinct()
                        .ToList(),




                    // Lấy ảnh thứ 2, nếu không có thì null
                    heroImage = p.Images
                        .Select(img => img.ImageUrl)
                        .ElementAtOrDefault(1),

                    // Lấy list từ ảnh thứ 2 trở đi (nếu không có thì list rỗng)
                    ProductImage = p.Images

                        .Skip(1)
                        .Select(img => img.ImageUrl)
                        .ToList(),

                    Sizes = p.ProductSizes
                        .Select(ps => new SizeWithColorsDto
                        {
                            SizeName = ps.Size != null
                                ? ps.Size.Name
                                : ps.CustomValue,

                            Color = ps.ProductColors
                                .Select(pc => new ColorAndquantity
                                {
                                    ColorCode = pc.ColorCode,
                                    quantity = pc.Quantity
                                })
                                .ToList()
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
    



    [HttpGet("couponsList")]
        public async Task<IActionResult> GetCouponsList()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return Ok(coupons);
        }
    }


    }


