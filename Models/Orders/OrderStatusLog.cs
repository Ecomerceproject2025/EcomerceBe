using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class OrderStatusLog
    {
        public int OrderStatusLogId { get; set; }
        public int OrderId { get; set; }
        [MaxLength(255)] public string PreviousStatus { get; set; }
        [MaxLength(255)] public string NewStatus { get; set; }
        [MaxLength(255)] public string? PreviousPaymentStatus { get; set; }
        [MaxLength(255)] public string? NewPaymentStatus { get; set; }
        [MaxLength(255)] public string? ActionType { get; set; } // "status_change", "cancel", "refund"
        [MaxLength(1000)] public string? Notes { get; set; }
        public int? ChangedByUserId { get; set; } // Admin user ID who made the change
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Order Order { get; set; }
        public User? ChangedByUser { get; set; }
    }
}

