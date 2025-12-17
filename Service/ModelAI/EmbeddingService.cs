using EcomerceBE.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace EcomerceBE.Service.ModelAI
{
    /// <summary>
    /// Service để generate embedding vectors từ AI model
    /// Có thể gọi Python API hoặc sử dụng ML.NET với ONNX model
    /// </summary>
    public class EmbeddingService : IEmbeddingService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly HttpClient _httpClient;

        // URL của AI model service (có thể là Python FastAPI service)
        private readonly string? _aiModelApiUrl;

        public EmbeddingService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<EmbeddingService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _aiModelApiUrl = _configuration["AIModel:ApiUrl"]; // Ví dụ: "http://localhost:8000"
            
            // Set timeout cho HTTP client
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Generate embedding bằng cách gọi AI model API
        /// Nếu không có API URL, sẽ trả về mock embedding (để test)
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be empty", nameof(text));
            }

            // Nếu có AI Model API URL, gọi API
            if (!string.IsNullOrEmpty(_aiModelApiUrl))
            {
                try
                {
                    var requestBody = new
                    {
                        text = text
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"{_aiModelApiUrl}/api/embedding/generate", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result?.Embedding != null && result.Embedding.Length > 0)
                        {
                            return result.Embedding;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"AI Model API returned {response.StatusCode}. Falling back to mock embedding.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling AI Model API. Falling back to mock embedding.");
                }
            }

            // Fallback: Mock embedding (384 dimensions - all-MiniLM-L6-v2)
            // Trong production, nên throw exception hoặc sử dụng local model
            _logger.LogWarning("Using mock embedding. Please configure AI Model API URL in appsettings.json");
            return GenerateMockEmbedding(text, 384);
        }

        /// <summary>
        /// Generate mock embedding từ hash của text (để test khi chưa có AI model)
        /// </summary>
        private float[] GenerateMockEmbedding(string text, int dimension)
        {
            var embedding = new float[dimension];
            var hash = text.GetHashCode();
            var random = new Random(hash);

            for (int i = 0; i < dimension; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2 - 1); // Random value between -1 and 1
            }

            // Normalize vector
            var norm = Math.Sqrt(embedding.Sum(x => x * x));
            if (norm > 0)
            {
                for (int i = 0; i < dimension; i++)
                {
                    embedding[i] = (float)(embedding[i] / norm);
                }
            }

            return embedding;
        }

        /// <summary>
        /// Generate embeddings cho tất cả sản phẩm chưa có embedding
        /// </summary>
        public async Task<int> GenerateEmbeddingsForAllProductsAsync()
        {
            var productsWithoutEmbedding = await _context.Products
                .Where(p => p.IsActive 
                    && !_context.ProductEmbeddings.Any(pe => pe.ProductId == p.ProductId))
                .ToListAsync();

            int count = 0;

            foreach (var product in productsWithoutEmbedding)
            {
                try
                {
                    // Tạo text từ tên và mô tả sản phẩm
                    var text = $"{product.Name} {product.Description ?? ""}";
                    
                    // Generate embedding
                    var embedding = await GenerateEmbeddingAsync(text);
                    
                    // Lưu vào database
                    var productEmbedding = new Models.ModelAI.ProductEmbedding
                    {
                        ProductId = product.ProductId,
                        EmbeddingVector = JsonSerializer.Serialize(embedding),
                        EmbeddingDimension = embedding.Length,
                        ModelName = "sentence-transformers/all-MiniLM-L6-v2",
                        ModelVersion = "1.0",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ProductEmbeddings.Add(productEmbedding);
                    count++;

                    // Save mỗi 10 sản phẩm để tránh timeout
                    if (count % 10 == 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Generated embeddings for {count} products...");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error generating embedding for product {product.ProductId}");
                }
            }

            // Save remaining products
            if (count % 10 != 0)
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Completed generating embeddings for {count} products.");
            return count;
        }

        /// <summary>
        /// Update embedding cho một sản phẩm cụ thể
        /// </summary>
        public async Task UpdateEmbeddingForProductAsync(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product {productId} not found");
            }

            // Tạo text từ tên và mô tả
            var text = $"{product.Name} {product.Description ?? ""}";
            
            // Generate embedding
            var embedding = await GenerateEmbeddingAsync(text);

            // Tìm hoặc tạo ProductEmbedding
            var productEmbedding = await _context.ProductEmbeddings
                .FirstOrDefaultAsync(pe => pe.ProductId == productId);

            if (productEmbedding == null)
            {
                productEmbedding = new Models.ModelAI.ProductEmbedding
                {
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductEmbeddings.Add(productEmbedding);
            }

            // Update embedding
            productEmbedding.EmbeddingVector = JsonSerializer.Serialize(embedding);
            productEmbedding.EmbeddingDimension = embedding.Length;
            productEmbedding.ModelName = "sentence-transformers/all-MiniLM-L6-v2";
            productEmbedding.ModelVersion = "1.0";
            productEmbedding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Updated embedding for product {productId}");
        }

        /// <summary>
        /// Response model từ AI Model API
        /// </summary>
        private class EmbeddingResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
            public int Dimension { get; set; }
        }
    }
}

