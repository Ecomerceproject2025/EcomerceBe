using System.Security.Claims;
using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        // DTOs
        public class CreateReviewDto
        {
            public int ProductId { get; set; }
            public int? OrderItemId { get; set; }
            public int Rating { get; set; } // 1-5
            public string Comment { get; set; } = string.Empty;
            public List<string>? ImageUrls { get; set; } // Base64 or URLs
        }

        public class ReviewResponseDto
        {
            public int ReviewId { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string? UserAvatar { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int Rating { get; set; }
            public string Comment { get; set; } = string.Empty;
            public List<string> ImageUrls { get; set; } = new();
            public DateTime CreatedAt { get; set; }
            public List<ReplyResponseDto> Replies { get; set; } = new();
        }

        public class ReplyResponseDto
        {
            public int ReviewReplyId { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string ReplyText { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        // POST /api/review - Create a review
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            // Validate rating
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5." });

            // Check if product exists
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            // If OrderItemId is provided, verify it belongs to the user and order is delivered
            if (dto.OrderItemId.HasValue)
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .FirstOrDefaultAsync(oi => oi.OrderItemId == dto.OrderItemId.Value);

                if (orderItem == null)
                    return NotFound(new { message = "Order item not found." });

                if (orderItem.Order.UserId != userId)
                    return StatusCode(403, new { message = "You can only review your own orders." });

                if (orderItem.Order.OrderStatus != "Delivered")
                    return BadRequest(new { message = "You can only review delivered orders." });

                // Check if user already reviewed this order item
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.OrderItemId == dto.OrderItemId.Value && r.UserId == userId);

                if (existingReview != null)
                    return BadRequest(new { message = "You have already reviewed this order item." });
            }

            // Check if user already reviewed this product (if no OrderItemId)
            if (!dto.OrderItemId.HasValue)
            {
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == dto.ProductId && r.UserId == userId);

                if (existingReview != null)
                    return BadRequest(new { message = "You have already reviewed this product." });
            }

            // Create review within a transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var review = new Review
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    OrderItemId = dto.OrderItemId,
                    Rating = dto.Rating,
                    Comment = dto.Comment ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Add images if provided (try-catch to handle if ReviewImages table doesn't exist)
                if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
                {
                    try
                    {
                        foreach (var imageUrl in dto.ImageUrls)
                        {
                            if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                            var reviewImage = new ReviewImage
                            {
                                ReviewId = review.ReviewId,
                                ImageUrl = imageUrl,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ReviewImages.Add(reviewImage);
                        }
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception imgEx)
                    {
                        // Log but don't fail the review creation if images fail
                        // This allows reviews to be created even if ReviewImages table doesn't exist yet
                        System.Diagnostics.Debug.WriteLine($"Warning: Failed to save review images: {imgEx.Message}");
                    }
                }

                await transaction.CommitAsync();

                // Update product average rating (outside transaction)
                await UpdateProductRating(dto.ProductId);

                return Ok(new { message = "Review created successfully.", reviewId = review.ReviewId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = "Failed to create review.";
                var detail = ex.Message;
                
                // Check if it's a MySQL table not found error
                if (ex.Message.Contains("Table") && (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown")))
                {
                    errorMessage = "Database table not found. Please run migrations or create the ReviewImages table.";
                    detail = "The ReviewImages table may not exist in the database. Please check the database schema or run the SQL script in Scripts/CreateReviewImagesTable.sql";
                }
                
                return StatusCode(500, new { message = errorMessage, detail = detail });
            }
        }

        // GET /api/review/product/{productId} - Get reviews for a product
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductReviews(int productId, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ReviewImages)
                .Include(r => r.Replies)
                    .ThenInclude(rr => rr.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(r => new ReviewResponseDto
                {
                    ReviewId = r.ReviewId,
                    UserId = r.UserId,
                    UserName = r.User.Name ?? "Ẩn danh",
                    UserAvatar = r.User.Avatar,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    ImageUrls = r.ReviewImages.Select(ri => ri.ImageUrl).ToList(),
                    CreatedAt = r.CreatedAt,
                    Replies = r.Replies.Select(rr => new ReplyResponseDto
                    {
                        ReviewReplyId = rr.ReviewReplyId,
                        UserId = rr.UserId,
                        UserName = rr.User.Name ?? "Admin",
                        ReplyText = rr.ReplyText,
                        CreatedAt = rr.CreatedAt
                    }).ToList()
                })
                .ToListAsync();

            var total = await _context.Reviews.CountAsync(r => r.ProductId == productId);

            return Ok(new
            {
                data = reviews,
                total,
                page,
                limit,
                totalPages = (int)Math.Ceiling(total / (double)limit)
            });
        }

        // GET /api/review/order/{orderId} - Get reviews for an order
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderReviews(int orderId)
        {
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            // Verify order belongs to user
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (order.UserId != userId)
                return StatusCode(403, new { message = "You can only view reviews for your own orders." });

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ReviewImages)
                .Include(r => r.OrderItem)
                .Where(r => r.OrderItem != null && r.OrderItem.OrderId == orderId)
                .Select(r => new ReviewResponseDto
                {
                    ReviewId = r.ReviewId,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    UserAvatar = r.User.Avatar,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ImageUrls = r.ReviewImages.Select(ri => ri.ImageUrl).ToList(),
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // DELETE /api/review/{id} - Delete a review (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.ReviewImages)
                .Include(r => r.Replies)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
                return NotFound(new { message = "Review not found." });

            // Delete related images and replies (cascade)
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Update product rating after deletion
            await UpdateProductRating(review.ProductId);

            return Ok(new { message = "Review deleted successfully." });
        }

        // POST /api/review/{id}/reply - Reply to a review (Admin only)
        [HttpPost("{id}/reply")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplyToReview(int id, [FromBody] ReplyDto dto)
        {
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = "Review not found." });

            if (string.IsNullOrWhiteSpace(dto.ReplyText))
                return BadRequest(new { message = "Reply text is required." });

            var reply = new ReviewReply
            {
                ReviewId = id,
                UserId = userId,
                ReplyText = dto.ReplyText.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewReplies.Add(reply);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reply added successfully.", replyId = reply.ReviewReplyId });
        }

        public class ReplyDto
        {
            public string ReplyText { get; set; } = string.Empty;
        }

        // Helper method to update product rating
        private async Task UpdateProductRating(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            if (reviews.Count > 0)
            {
                var averageRating = reviews.Average(r => r.Rating);
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.StarRating = (double)averageRating;
                    _context.Products.Update(product);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}

