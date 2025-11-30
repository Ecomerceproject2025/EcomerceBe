using EcomerceBE.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static EcomerceBE.Controllers.ProductViewController;

namespace EcomerceBE.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductViewController : Controller
    {


        private readonly AppDbContext _context;

        public ProductViewController(AppDbContext context)
        {
            _context = context;
        }


        public class ExploreProductDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public List<string> image { get; set; } = new();
            public decimal Price { get; set; }
            public string Description { get; set; } = "";
            public double StarRatingrating { get; set; }
            public int ReviewCounts { get; set; }
            public int totalquantity { get; set; }
        }


        public class ProductDetailDto : ExploreProductDto
        {
            public List<SizeWithColorsDto> Sizes { get; set; } = new();
            public List<ReviewDto> Reviews { get; set; } = new();
            public int? ReturnDeliveryDay { get; set; }    
        }



        public class ColorAndquantity
        {
            public string ColorCode { get; set; }
            public int quantity { get; set; }
        }
        public class SizeWithColorsDto
        {
            public string SizeName { get; set; } = "";
            public List<ColorAndquantity> Color { get; set; }
        }

        public class ReviewDto
        {
            public string Comment { get; set; }
            public int Rating { get; set; }
            public DateTime CreatedAt { get; set; }
            public string UserName { get; set; }
            public int userid { get; set; }
            public string Avatar { get; set; }
        }

        [HttpGet("ExploreOurProducts")]
        public async Task<IActionResult> ExploreOurProducts([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;

            var baseQuery = _context.Products.AsNoTracking();

            var total = await baseQuery.CountAsync();

            var data = await baseQuery
                .OrderBy(p => p.ProductId)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new ExploreProductDto
                {
                    Id = p.ProductId,
                    Name = p.Name,
                    image = p.Images.Select(i => i.ImageUrl).ToList(),
                    Price = p.Price,
                    Description = p.Description, // sửa chính tả
                    StarRatingrating = p.StarRating,

                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / limit);

            return Ok(new { total, page, limit, totalPages, data });
        }

        [HttpGet("GetProductCateGory/{id:int}")]
        public async Task<IActionResult> GetProductCateGory(int id, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;

            var baseQuery = _context.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == id);

            var total = await baseQuery.CountAsync();

            if (total == 0)
            {
                return Ok(new { total = 0, page, limit, totalPages = 0, data = new List<object>() });
            }

            var data = await baseQuery  // 🔥 Đổi Productlist thành data
                .OrderBy(p => p.ProductId)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new ExploreProductDto
                {
                    Id = p.ProductId,
                    Name = p.Name,
                    image = p.Images.Select(i => i.ImageUrl).ToList(),
                    Price = p.Price,
                    Description = p.Description,
                    StarRatingrating = p.StarRating,
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / limit);

            return Ok(new { total, page, limit, totalPages, data }); // 🔥 Trả về data
        }


        [HttpGet("GetProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.ProductId == id)
                .Select(p => new ProductDetailDto
                {
                    Id = p.ProductId,
                    Name = p.Name,
                    image = p.Images.Select(i => i.ImageUrl).ToList(),
                    Price = p.Price,
                    Description = p.Description,
                    StarRatingrating = p.StarRating,

                    ReturnDeliveryDay= p.ReturnDeliveryDay,
                    // Mỗi size có list màu riêng
                    Sizes = p.ProductSizes
                        .Select(ps => new SizeWithColorsDto
                        {


                            SizeName = ps.Size != null ? ps.Size.Name : ps.CustomValue,
                            Color= ps.ProductColors
                                .Select(pc => new ColorAndquantity
                                {
                                    ColorCode = pc.ColorCode,
                                    quantity = pc.Quantity
                                })
                                .ToList(),

                        })
                        .ToList(),


                    totalquantity = p.ProductSizes
                        .SelectMany(ps => ps.ProductColors)
                        .Sum(pc => pc.Quantity),


                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }
            return Ok(product);


        }










      
    


     [HttpGet("GetReviewsByProductId/{id}")]
        public async Task<IActionResult> GetReviewsByProductId(int id)
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.ProductId == id)
                .Select(r => new ReviewDto
                {
                    Comment = r.Comment,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    UserName = r.User.Name,
                    userid = r.UserId,
                    Avatar = r.User.Avatar,
                })
                .ToListAsync();
            return Ok(reviews);
        }

    } }



