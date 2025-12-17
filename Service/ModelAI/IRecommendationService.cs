using EcomerceBE.DTOs.ModelAI;

namespace EcomerceBE.Service.ModelAI
{
    /// <summary>
    /// Service để tính similarity và trả về product recommendations
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Lấy sản phẩm gợi ý dựa trên sản phẩm hiện tại (Content-Based)
        /// </summary>
        /// <param name="productId">ID sản phẩm hiện tại</param>
        /// <param name="topK">Số lượng sản phẩm gợi ý (mặc định: 10)</param>
        /// <returns>Danh sách sản phẩm gợi ý với similarity score</returns>
        Task<List<RecommendationResponse>> GetContentBasedRecommendationsAsync(
            int productId, 
            int topK = 10);
        
        /// <summary>
        /// Lấy sản phẩm gợi ý dựa trên lịch sử người dùng (User-Based)
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <param name="topK">Số lượng sản phẩm gợi ý (mặc định: 10)</param>
        /// <returns>Danh sách sản phẩm gợi ý với similarity score</returns>
        Task<List<RecommendationResponse>> GetUserBasedRecommendationsAsync(
            int userId, 
            int topK = 10);
    }
}

