using EcomerceBE.DTOs.ModelAI;
using EcomerceBE.Service.ModelAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcomerceBE.Controllers.ModelAI
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IRecommendationService recommendationService,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy sản phẩm gợi ý dựa trên sản phẩm hiện tại hoặc lịch sử người dùng
        /// </summary>
        /// <param name="request">Request chứa productId, userId, topK, recommendationType</param>
        /// <returns>Danh sách sản phẩm gợi ý</returns>
        [HttpPost("get-recommendations")]
        public async Task<IActionResult> GetRecommendations(
            [FromBody] GetRecommendationsRequest request)
        {
            try
            {
                List<RecommendationResponse> recommendations;

                if (request.RecommendationType == "content_based")
                {
                    if (!request.ProductId.HasValue)
                    {
                        return BadRequest(new { message = "ProductId is required for content-based recommendations" });
                    }

                    recommendations = await _recommendationService
                        .GetContentBasedRecommendationsAsync(
                            request.ProductId.Value, 
                            request.TopK);
                }
                else if (request.RecommendationType == "user_based")
                {
                    // Nếu không có userId trong request, lấy từ JWT token
                    int? userId = request.UserId;
                    
                    if (!userId.HasValue)
                    {
                        // Thử lấy từ JWT token (nếu có)
                        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int jwtUserId))
                        {
                            userId = jwtUserId;
                        }
                    }

                    if (!userId.HasValue)
                    {
                        return BadRequest(new { message = "UserId is required for user-based recommendations" });
                    }

                    recommendations = await _recommendationService
                        .GetUserBasedRecommendationsAsync(
                            userId.Value, 
                            request.TopK);
                }
                else
                {
                    return BadRequest(new { 
                        message = "RecommendationType must be 'content_based' or 'user_based'" 
                    });
                }

                return Ok(new
                {
                    recommendations,
                    recommendationType = request.RecommendationType,
                    totalCount = recommendations.Count
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy sản phẩm gợi ý dựa trên sản phẩm hiện tại (Content-Based) - Simplified endpoint
        /// </summary>
        [HttpGet("content-based/{productId}")]
        public async Task<IActionResult> GetContentBasedRecommendations(
            int productId,
            [FromQuery] int topK = 10)
        {
            try
            {
                var recommendations = await _recommendationService
                    .GetContentBasedRecommendationsAsync(productId, topK);

                return Ok(new
                {
                    recommendations,
                    recommendationType = "content_based",
                    totalCount = recommendations.Count
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content-based recommendations");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy sản phẩm gợi ý dựa trên lịch sử người dùng (User-Based) - Simplified endpoint
        /// </summary>
        [HttpGet("user-based")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> GetUserBasedRecommendations(
            [FromQuery] int topK = 10)
        {
            try
            {
                // Lấy UserId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var recommendations = await _recommendationService
                    .GetUserBasedRecommendationsAsync(userId, topK);

                return Ok(new
                {
                    recommendations,
                    recommendationType = "user_based",
                    totalCount = recommendations.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user-based recommendations");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}

