namespace EcomerceBE.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int? OrderItemId { get; set; } // Link to order item for tracking
        public int Rating { get; set; } // e.g., 1..5
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Product Product { get; set; }
        public OrderItem? OrderItem { get; set; }
        public ICollection<ReviewImage> ReviewImages { get; set; } = new List<ReviewImage>();
        public ICollection<ReviewReply> Replies { get; set; } = new List<ReviewReply>();
    }

    public class ReviewReply
    {
        public int ReviewReplyId { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; } // Admin who replied
        public string ReplyText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Review Review { get; set; }
        public User User { get; set; }
    }

    public class ReviewImage
    {
        public int ReviewImageId { get; set; }
        public int ReviewId { get; set; }
        public string ImageUrl { get; set; } = string.Empty; // Base64 or URL
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Review Review { get; set; }
    }
}
