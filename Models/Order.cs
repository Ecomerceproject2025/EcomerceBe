using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public int? AddressId { get; set; }
        public decimal TotalAmount { get; set; }
        public int? CouponId { get; set; }
        public decimal DiscountAmount { get; set; }
        [MaxLength(255)] public string PaymentMethod { get; set; }
        [MaxLength(255)] public string PaymentStatus { get; set; }
        [MaxLength(255)] public string OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Address Address { get; set; }
        public Coupon Coupon { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
