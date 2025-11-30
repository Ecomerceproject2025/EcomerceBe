using EcomerceBE.Models;
using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Coupon
    {
        public int CouponId { get; set; }
        [MaxLength(255)] public string Code { get; set; }
        public string Description { get; set; }
        public decimal DiscountValue { get; set; }
        public bool IsPercent { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int UsageCount { get; set; }
        public int MaxUsage { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Order> Orders { get; set; }
        public ICollection<ProductCoupon> ProductCoupons { get; set; }
        public ICollection<UserCoupon> UserCoupons { get; set; }
    }

        
}

public class ProductCoupon
{
    public int ProductId { get; set; }
    public Product Product { get; set; }

    public int CouponId { get; set; }
    public Coupon Coupon { get; set; }
}


public class UserCoupon
{
    public int UserId { get; set; }
    public User User { get; set; }

    public int CouponId { get; set; }
    public Coupon Coupon { get; set; }

    public int UsedCount { get; set; } = 0;
}

