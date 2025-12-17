using EcomerceBE.Service.ModelAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomerceBE.Controllers.ModelAI
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được generate embeddings
    public class EmbeddingController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<EmbeddingController> _logger;

        public EmbeddingController(
            IEmbeddingService embeddingService,
            ILogger<EmbeddingController> logger)
        {
            _embeddingService = embeddingService;
            _logger = logger;
        }

        /// <summary>
        /// Generate embeddings cho tất cả sản phẩm chưa có embedding
        /// </summary>
        /// <returns>Số lượng embeddings đã được generate</returns>
        [HttpPost("generate-all")]
        public async Task<IActionResult> GenerateAllEmbeddings()
        {
            try
            {
                _logger.LogInformation("Starting to generate embeddings for all products...");
                
                var count = await _embeddingService.GenerateEmbeddingsForAllProductsAsync();
                
                _logger.LogInformation($"Successfully generated {count} embeddings");
                
                return Ok(new
                {
                    message = $"Successfully generated embeddings for {count} products",
                    count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings for all products");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate hoặc update embedding cho một sản phẩm cụ thể
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <returns></returns>
        [HttpPost("generate/{productId}")]
        public async Task<IActionResult> GenerateEmbeddingForProduct(int productId)
        {
            try
            {
                _logger.LogInformation($"Generating embedding for product {productId}...");
                
                await _embeddingService.UpdateEmbeddingForProductAsync(productId);
                
                _logger.LogInformation($"Successfully generated embedding for product {productId}");
                
                return Ok(new
                {
                    message = $"Successfully generated embedding for product {productId}",
                    productId = productId
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Product {productId} not found");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating embedding for product {productId}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Test generate embedding từ text (để test AI Model API)
        /// </summary>
        /// <param name="text">Text cần mã hóa</param>
        /// <returns>Embedding vector và dimension</returns>
        [HttpPost("test")]
        public async Task<IActionResult> TestGenerateEmbedding([FromBody] string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return BadRequest(new { message = "Text cannot be empty" });
                }

                _logger.LogInformation($"Testing embedding generation for text: {text.Substring(0, Math.Min(50, text.Length))}...");
                
                var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
                
                return Ok(new
                {
                    message = "Embedding generated successfully",
                    dimension = embedding.Length,
                    sample = embedding.Take(5).ToArray(), // Chỉ trả về 5 giá trị đầu để preview
                    note = "This is a test endpoint. Full embedding vector is not returned."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing embedding generation");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}

