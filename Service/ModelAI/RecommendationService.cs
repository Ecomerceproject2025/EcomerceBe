using EcomerceBE.Data;
using EcomerceBE.DTOs.ModelAI;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EcomerceBE.Service.ModelAI
{
    /// <summary>
    /// Service để tính similarity và trả về product recommendations
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            AppDbContext context,
            ILogger<RecommendationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tính cosine similarity giữa 2 vectors
        /// </summary>
        private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null)
                throw new ArgumentNullException("Vectors cannot be null");

            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vectors must have same dimension");

            double dotProduct = 0;
            double norm1 = 0;
            double norm2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                norm1 += vector1[i] * vector1[i];
                norm2 += vector2[i] * vector2[i];
            }

            var denominator = Math.Sqrt(norm1) * Math.Sqrt(norm2);
            if (denominator == 0)
                return 0;

            return dotProduct / denominator;
        }

        /// <summary>
        /// Parse embedding vector từ JSON string
        /// </summary>
        private float[] ParseEmbeddingVector(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                throw new ArgumentException("Embedding vector cannot be empty");

            try
            {
                var vector = JsonSerializer.Deserialize<float[]>(jsonString);
                return vector ?? throw new InvalidOperationException("Invalid embedding vector format");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing embedding vector");
                throw new InvalidOperationException("Invalid embedding vector format", ex);
            }
        }

        /// <summary>
        /// Lấy sản phẩm gợi ý dựa trên sản phẩm hiện tại (Content-Based)
        /// </summary>
        public async Task<List<RecommendationResponse>> GetContentBasedRecommendationsAsync(
            int productId, 
            int topK = 10)
        {
            // 1. Kiểm tra sản phẩm có tồn tại không
            var productExists = await _context.Products
                .AnyAsync(p => p.ProductId == productId && p.IsActive);

            if (!productExists)
            {
                _logger.LogWarning($"Product {productId} not found or not active");
                return new List<RecommendationResponse>();
            }

            // 2. Lấy embedding của sản phẩm hiện tại
            var currentProductEmbedding = await _context.ProductEmbeddings
                .FirstOrDefaultAsync(pe => pe.ProductId == productId);

            if (currentProductEmbedding == null)
            {
                _logger.LogWarning($"Product embedding not found for product {productId}");
                return new List<RecommendationResponse>();
            }

            var currentVector = ParseEmbeddingVector(currentProductEmbedding.EmbeddingVector);

            // 3. Lấy tất cả embeddings khác (chỉ sản phẩm active)
            var allEmbeddings = await _context.ProductEmbeddings
                .Where(pe => pe.ProductId != productId)
                .Include(pe => pe.Product)
                    .ThenInclude(p => p.Images)
                .Include(pe => pe.Product)
                    .ThenInclude(p => p.Reviews)
                .Where(pe => pe.Product.IsActive)
                .ToListAsync();

            if (!allEmbeddings.Any())
            {
                _logger.LogWarning("No other products with embeddings found");
                return new List<RecommendationResponse>();
            }

            // 4. Tính similarity và sắp xếp
            var similarities = allEmbeddings
                .Select(pe =>
                {
                    try
                    {
                        var otherVector = ParseEmbeddingVector(pe.EmbeddingVector);
                        var similarity = CalculateCosineSimilarity(currentVector, otherVector);
                        return new
                        {
                            ProductEmbedding = pe,
                            Similarity = similarity
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error calculating similarity for product {pe.ProductId}");
                        return null;
                    }
                })
                .Where(x => x != null)
                .OrderByDescending(x => x!.Similarity)
                .Take(topK)
                .ToList();

            // 5. Map sang RecommendationResponse
            var recommendations = similarities!
                .Select(x =>
                {
                    var product = x.ProductEmbedding.Product;
                    var heroImage = product.Images?.FirstOrDefault()?.ImageUrl;
                    var starRating = product.StarRating > 0 
                        ? product.StarRating 
                        : (product.Reviews?.Any() == true 
                            ? product.Reviews.Average(r => r.Rating) 
                            : 0);

                    return new RecommendationResponse
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        DiscountPrice = product.DiscountPrice > 0 ? product.DiscountPrice : null,
                        HeroImage = heroImage,
                        StarRating = Math.Round(starRating, 1),
                        SimilarityScore = Math.Round(x.Similarity, 4)
                    };
                })
                .ToList();

            _logger.LogInformation($"Generated {recommendations.Count} content-based recommendations for product {productId}");
            return recommendations;
        }

        /// <summary>
        /// Lấy sản phẩm gợi ý dựa trên lịch sử người dùng (User-Based)
        /// </summary>
        public async Task<List<RecommendationResponse>> GetUserBasedRecommendationsAsync(
            int userId, 
            int topK = 10)
        {
            // 1. Lấy các sản phẩm user đã xem/mua
            var userBehaviors = await _context.UserBehaviorLogs
                .Where(ubl => ubl.UserId == userId 
                    && (ubl.BehaviorType == "view" || ubl.BehaviorType == "purchase"))
                .Include(ubl => ubl.Product)
                .Where(ubl => ubl.Product.IsActive)
                .ToListAsync();

            if (!userBehaviors.Any())
            {
                _logger.LogInformation($"No user behavior found for user {userId}");
                return new List<RecommendationResponse>();
            }

            // 2. Lấy embeddings của các sản phẩm user đã tương tác
            var viewedProductIds = userBehaviors.Select(ubl => ubl.ProductId).Distinct().ToList();
            
            var userProductEmbeddings = await _context.ProductEmbeddings
                .Where(pe => viewedProductIds.Contains(pe.ProductId))
                .ToListAsync();

            if (!userProductEmbeddings.Any())
            {
                _logger.LogWarning($"No product embeddings found for user {userId}'s interacted products");
                return new List<RecommendationResponse>();
            }

            // 3. Tính embedding trung bình của user (user profile)
            var userEmbeddings = new List<float[]>();
            foreach (var item in userProductEmbeddings)
            {
                try
                {
                    var vector = ParseEmbeddingVector(item.EmbeddingVector);
                    userEmbeddings.Add(vector);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error parsing embedding for product {item.ProductId}");
                }
            }

            if (!userEmbeddings.Any())
            {
                return new List<RecommendationResponse>();
            }

            // Tính vector trung bình (user profile)
            var dimension = userEmbeddings[0].Length;
            var userVector = new float[dimension];
            
            foreach (var embedding in userEmbeddings)
            {
                for (int i = 0; i < dimension; i++)
                {
                    userVector[i] += embedding[i];
                }
            }

            // Normalize
            for (int i = 0; i < dimension; i++)
            {
                userVector[i] /= userEmbeddings.Count;
            }

            // 4. Tìm sản phẩm tương đồng với user profile (loại bỏ sản phẩm đã xem)

            var allEmbeddings = await _context.ProductEmbeddings
                .Where(pe => !viewedProductIds.Contains(pe.ProductId))
                .Include(pe => pe.Product)
                    .ThenInclude(p => p.Images)
                .Include(pe => pe.Product)
                    .ThenInclude(p => p.Reviews)
                .Where(pe => pe.Product.IsActive)
                .ToListAsync();

            if (!allEmbeddings.Any())
            {
                _logger.LogInformation("No other products with embeddings found for user-based recommendations");
                return new List<RecommendationResponse>();
            }

            // 5. Tính similarity với user profile
            var similarities = allEmbeddings
                .Select(pe =>
                {
                    try
                    {
                        var productVector = ParseEmbeddingVector(pe.EmbeddingVector);
                        var similarity = CalculateCosineSimilarity(userVector, productVector);
                        return new
                        {
                            ProductEmbedding = pe,
                            Similarity = similarity
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error calculating similarity for product {pe.ProductId}");
                        return null;
                    }
                })
                .Where(x => x != null)
                .OrderByDescending(x => x!.Similarity)
                .Take(topK)
                .ToList();

            // 6. Map sang RecommendationResponse
            var recommendations = similarities!
                .Select(x =>
                {
                    var product = x.ProductEmbedding.Product;
                    var heroImage = product.Images?.FirstOrDefault()?.ImageUrl;
                    var starRating = product.StarRating > 0 
                        ? product.StarRating 
                        : (product.Reviews?.Any() == true 
                            ? product.Reviews.Average(r => r.Rating) 
                            : 0);

                    return new RecommendationResponse
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        DiscountPrice = product.DiscountPrice > 0 ? product.DiscountPrice : null,
                        HeroImage = heroImage,
                        StarRating = Math.Round(starRating, 1),
                        SimilarityScore = Math.Round(x.Similarity, 4)
                    };
                })
                .ToList();

            _logger.LogInformation($"Generated {recommendations.Count} user-based recommendations for user {userId}");
            return recommendations;
        }
    }
}

