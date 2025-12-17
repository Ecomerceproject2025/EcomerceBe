using System;
using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models.ModelAI
{
    /// <summary>
    /// Model để lưu trữ hành vi người dùng (xem, mua, thêm vào giỏ hàng)
    /// </summary>
    public class UserBehaviorLog
    {
        [Key]
        public int UserBehaviorLogId { get; set; }

        /// <summary>
        /// ID người dùng (có thể null nếu chưa đăng nhập)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// ID sản phẩm
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Loại hành vi: "view", "add_to_cart", "purchase", "wishlist"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string BehaviorType { get; set; } = string.Empty;

        /// <summary>
        /// Thời gian xem sản phẩm (tính bằng giây) - chỉ áp dụng cho behaviorType = "view"
        /// </summary>
        public int? ViewDuration { get; set; }

        /// <summary>
        /// Session ID để nhóm các hành vi trong cùng một phiên
        /// </summary>
        [MaxLength(255)]
        public string? SessionId { get; set; }

        /// <summary>
        /// IP Address của người dùng
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent (browser, device info)
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Thời gian ghi nhận hành vi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Metadata bổ sung (JSON string) - có thể lưu thông tin như: referrer, category, etc.
        /// </summary>
        public string? Metadata { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Product Product { get; set; } = null!;
    }
}

