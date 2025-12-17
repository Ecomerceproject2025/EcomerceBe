using System;
using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models.ModelAI
{
    /// <summary>
    /// Model để lưu trữ embedding vector của sản phẩm
    /// Embedding được tạo từ mô hình BERT (Sentence-BERT hoặc MiniLM)
    /// </summary>
    public class ProductEmbedding
    {
        [Key]
        public int ProductEmbeddingId { get; set; }

        /// <summary>
        /// ID sản phẩm (1-1 relationship với Product)
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Embedding vector dưới dạng JSON string hoặc binary
        /// Format: JSON array of floats [0.123, 0.456, ...]
        /// Hoặc có thể lưu dưới dạng binary BLOB
        /// </summary>
        [Required]
        public string EmbeddingVector { get; set; } = string.Empty;

        /// <summary>
        /// Số chiều của embedding vector (384, 768, etc.)
        /// </summary>
        [Required]
        public int EmbeddingDimension { get; set; }

        /// <summary>
        /// Tên mô hình đã sử dụng để tạo embedding (ví dụ: "sentence-transformers/all-MiniLM-L6-v2")
        /// </summary>
        [MaxLength(255)]
        public string? ModelName { get; set; }

        /// <summary>
        /// Version của mô hình
        /// </summary>
        [MaxLength(50)]
        public string? ModelVersion { get; set; }

        /// <summary>
        /// Thời gian tạo embedding
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian cập nhật embedding (khi sản phẩm thay đổi)
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Product Product { get; set; } = null!;
    }
}

