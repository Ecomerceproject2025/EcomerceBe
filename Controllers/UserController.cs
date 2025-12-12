using EcomerceBE.Data;
using EcomerceBE.DTOs;
using EcomerceBE.Models;
using EcomerceBE.Service.user;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static EcomerceBE.Controllers.ProductViewController;

namespace EcomerceBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _context;
        // ✅ Dùng interface thay vì class cụ thể
        public UserController(IUserService userService, AppDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetUserInfo()
        {
            try
            {
                var userId = User.FindFirst("id")?.Value;
                var role = User.FindFirst("role")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Token Info not match.");

                var user = _userService.GetById(int.Parse(userId));
                if (user == null)
                    return NotFound("Not found user");

                return Ok(new
                {
                    Id = user.Id,
                    Role = user.Role,
                    Name = user.Name,
                    Email = user.Email,
                    Avatar = user.Avatar
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetUserInfo failed: {ex.Message}");
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }
        [Authorize]
        [HttpGet("profile/{userId:int}")]
        public async Task<IActionResult> GetUserById([FromRoute] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid user id.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            return Ok(new
            {
                Name = user.Name,
                Email = user.Email,
                Avatar = user.Avatar,
                Status = user.status
            });
        }



        [Authorize]
        [HttpPost("add_address")]
        public async Task<IActionResult> AddAddress([FromBody] AddAddress dto)
        {
            try
            {
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Token Info not match.");

                var user = _userService.GetById(int.Parse(userId));
                if (user == null)
                    return NotFound("Not found user");

                var addressCount = await _context.Addresses.CountAsync(a => a.UserId == int.Parse(userId));
                if (addressCount>5)
                {
                    return BadRequest("You have reached the maximum number of addresses allowed.");
                }



                var splitAddress = AddressParser.Parse(dto.address);
                var newAddress = new Address
                {
                    Street = splitAddress.Street ?? "",
                    Province = splitAddress.Province ?? "",
                    City = splitAddress.Ward ?? "",
                    PostalCode = splitAddress.PostalCode ?? "",
                    Country = splitAddress.Country ?? "",
                    Apartment = splitAddress.Note ?? "",
                    Phone = dto.phone,
                    IsDefault= false,
                    UserId = user.Id


                };
                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Address added successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AddAddress failed: {ex.Message}");
                return StatusCode(500, "Bug server");
            }
        }


        [Authorize]
        [HttpGet("Get_address")]
        public async Task<IActionResult> GetAddress()
        {
            try
            {
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Token Info not match.");

                var user = _userService.GetById(int.Parse(userId));
                if (user == null)
                    return NotFound("Not found user");
                var addressList = await _context.Addresses
                    .Where(a => a.UserId == int.Parse(userId))
                    .Select(a => new
                    {
                        a.AddressId,
                        a.Street,
                        a.Apartment,
                        a.City,
                        a.PostalCode,
                        a.Country,
                        a.Phone,
                        a.Province,
                        a.IsDefault
                        // nếu muốn thêm thông tin user:
                        // User = new { a.User.Id, a.User.Name, a.User.Email }
                    })
                    .ToListAsync();

                return Ok(addressList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AddAddress failed: {ex.Message}");
                return StatusCode(500, "Bug server");
            }
        }



        [Authorize]
        [HttpDelete("remove_address/{id:int}")]
        public async Task<IActionResult> RemoveAddress([FromRoute] int id)
        {
            try
            {
                
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Tìm địa chỉ thuộc user hiện tại
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == int.Parse(userId));

                if (address == null)
                    return NotFound(); // 404 nếu không thấy hoặc không thuộc user

                // Hard delete: xóa khỏi DB
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                return NoContent(); // 204 theo chuẩn DELETE
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting address");
            }
        }




     

        [Authorize]
        [HttpPatch("AddressDefault/{id:int}")]
        public async Task<IActionResult> SetDefaultAddress([FromRoute] int id)
        {
            try
            {
                var userIdStr = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized();

                // Tìm address muốn set default
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
                    
                if (address == null)
                    return NotFound();

                // Set tất cả address của user thành không default
                var userAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                foreach (var a in userAddresses)
                {
                    a.IsDefault = false;
                }

                // Set address này là default
                address.IsDefault = true;

                await _context.SaveChangesAsync();

                return Ok("address was default"); 
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error setting default address");
            }
        }


        [HttpGet("normal_products")]
        public async Task<IActionResult> GetNomalProduct(
              [FromQuery] int page = 1,
              [FromQuery] int limit = 10)
        {


            var normalQuery = _context.Products
                .Where(p =>
                    (p.productType == null || !p.productType.ToLower().Equals("flashsale"))
                    && p.IsActive);

            int totalProducts = await normalQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalProducts / (double)limit);
            var products = await normalQuery
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

                    productType = p.productType,


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

            return Ok(new
            {
                totalPages = totalPages,
                totalProducts = totalProducts,
                products = products,
                page = page,
            }
            );
        }


    }
}

