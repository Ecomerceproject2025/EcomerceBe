using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.DTOs.ModelAI
{
    /// <summary>
    /// DTO để client gửi hành vi người dùng lên server
    /// </summary>
    public class UserBehaviorRequest
    {
        /// <summary>
        /// ID sản phẩm (bắt buộc)
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Loại hành vi: "view", "add_to_cart", "purchase", "wishlist"
        /// </summary>
        [Required]
        [RegularExpression("^(view|add_to_cart|purchase|wishlist)$", 
            ErrorMessage = "BehaviorType must be one of: view, add_to_cart, purchase, wishlist")]
        public string BehaviorType { get; set; } = string.Empty;

        /// <summary>
        /// Thời gian xem sản phẩm (tính bằng giây) - chỉ áp dụng cho behaviorType = "view"
        /// </summary>
        public int? ViewDuration { get; set; }

        /// <summary>
        /// Session ID để nhóm các hành vi trong cùng một phiên
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Metadata bổ sung (JSON string) - có thể chứa thông tin như: referrer, category, etc.
        /// </summary>
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// DTO để trả về danh sách sản phẩm gợi ý
    /// </summary>
    public class RecommendationResponse
    {
        /// <summary>
        /// ID sản phẩm
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Giá sản phẩm
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Giá giảm (nếu có)
        /// </summary>
        public decimal? DiscountPrice { get; set; }

        /// <summary>
        /// URL hình ảnh chính
        /// </summary>
        public string? HeroImage { get; set; }

        /// <summary>
        /// Điểm đánh giá
        /// </summary>
        public double StarRating { get; set; }

        /// <summary>
        /// Độ tương đồng (similarity score) từ 0-1
        /// </summary>
        public double SimilarityScore { get; set; }
    }

    /// <summary>
    /// DTO để yêu cầu gợi ý sản phẩm
    /// </summary>
    public class GetRecommendationsRequest
    {
        /// <summary>
        /// ID sản phẩm hiện tại (để tìm sản phẩm tương tự)
        /// </summary>
        public int? ProductId { get; set; }

        /// <summary>
        /// ID người dùng (để gợi ý cá nhân hóa dựa trên lịch sử)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Số lượng sản phẩm gợi ý (mặc định: 10)
        /// </summary>
        [Range(1, 50)]
        public int TopK { get; set; } = 10;

        /// <summary>
        /// Loại gợi ý: "content_based" (dựa trên sản phẩm hiện tại) hoặc "user_based" (dựa trên lịch sử người dùng)
        /// </summary>
        [RegularExpression("^(content_based|user_based)$", 
            ErrorMessage = "RecommendationType must be one of: content_based, user_based")]
        public string RecommendationType { get; set; } = "content_based";
    }
}

