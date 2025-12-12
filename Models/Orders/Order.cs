using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        [MaxLength(100)] public string? OrderNumber { get; set; } // Frontend-generated order ID
        public int UserId { get; set; }
        public int? AddressId { get; set; }
        public int? ShippingMethodId { get; set; } // Selected shipping method
        public decimal ShippingCost { get; set; } = 0; // Shipping cost in VND
        public decimal TotalAmount { get; set; }
        public int? CouponId { get; set; }
        public decimal DiscountAmount { get; set; }
        [MaxLength(255)] public string PaymentMethod { get; set; }
        [MaxLength(255)] public string PaymentStatus { get; set; }
        [MaxLength(255)] public string OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Address Address { get; set; }
        public ShippingMethod? ShippingMethod { get; set; }
        public Coupon Coupon { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
