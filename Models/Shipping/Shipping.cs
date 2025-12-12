using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Shipping
    {
        public int ShippingId { get; set; }
        public int OrderId { get; set; }
        
        [MaxLength(100)] public string? TrackingNumber { get; set; }
        [MaxLength(100)] public string? Carrier { get; set; } // "Vietnam Post", "GHTK", "GHN", "J&T", etc.
        [MaxLength(50)] public string? ShippingMethod { get; set; } // "Standard", "Express", "Same Day"
        
        public DateTime? ShippedDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        
        [MaxLength(255)] public string? Status { get; set; } // "Pending", "In Transit", "Out for Delivery", "Delivered", "Failed"
        [MaxLength(1000)] public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation
        public Order Order { get; set; }
    }
}

