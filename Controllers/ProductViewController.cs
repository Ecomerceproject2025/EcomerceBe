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
            public string ColorCode { get; set; } = string.Empty;
            public int quantity { get; set; }
        }
        public class SizeWithColorsDto
        {
            public string SizeName { get; set; } = "";
            public List<ColorAndquantity> Color { get; set; } = new();
        }

        public class ReviewDto
        {
            public int ReviewId { get; set; }
            public string Comment { get; set; } = string.Empty;
            public int Rating { get; set; }
            public DateTime CreatedAt { get; set; }
            public string UserName { get; set; } = string.Empty;
            public int userid { get; set; }
            public string Avatar { get; set; } = string.Empty;
            public List<string> ImageUrls { get; set; } = new();
            public int ProductId { get; set; }
            public List<ReplyDto> Replies { get; set; } = new();
        }

        public class ReplyDto
        {
            public int ReviewReplyId { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string ReplyText { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        [HttpGet("ExploreOurProducts")]
        public async Task<IActionResult> ExploreOurProducts([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;

            var baseQuery = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && (p.productType == null || !p.productType.ToLower().Equals("flashsale")));

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
                .Where(p => p.CategoryId == id && p.IsActive && (p.productType == null || !p.productType.ToLower().Equals("flashsale")));

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
                .Where(p => p.ProductId == id && p.IsActive == true && !p.productType.ToLower().Equals("flashsale"))
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


                            SizeName = ps.Size != null ? ps.Size.Name : (ps.CustomValue ?? string.Empty),
                            Color= ps.ProductColors != null
                                ? ps.ProductColors
                                    .Select(pc => new ColorAndquantity
                                    {
                                        ColorCode = pc.ColorCode,
                                        quantity = pc.Quantity
                                    })
                                    .ToList()
                                : new List<ColorAndquantity>(),

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
                .Include(r => r.ReviewImages)
                .Include(r => r.Replies)
                    .ThenInclude(rr => rr.User)
                .Where(r => r.ProductId == id)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    Comment = r.Comment ?? string.Empty,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    UserName = r.User.Name ?? "Ẩn danh",
                    userid = r.UserId,
                    Avatar = r.User.Avatar ?? string.Empty,
                    ImageUrls = r.ReviewImages.Select(ri => ri.ImageUrl).ToList(),
                    ProductId = r.ProductId,
                    Replies = r.Replies.Select(rr => new ReplyDto
                    {
                        ReviewReplyId = rr.ReviewReplyId,
                        UserId = rr.UserId,
                        UserName = rr.User.Name ?? "Admin",
                        ReplyText = rr.ReplyText,
                        CreatedAt = rr.CreatedAt
                    }).ToList()
                })
                .ToListAsync();
            return Ok(reviews);
        }

    } }



