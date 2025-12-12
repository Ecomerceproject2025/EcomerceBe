using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Product
    {
        public Product()
        {
            ProductSizes = new HashSet<ProductSize>();
            Reviews = new HashSet<Review>();
            CartItems = new HashSet<CartItem>();
            OrderItems = new HashSet<OrderItem>();
            FlashSaleItems = new HashSet<FlashSaleItem>();
            WishlistItems = new HashSet<WishListItem>();
            Images = new HashSet<ProductImage>();
            Colors = new HashSet<ProductColor>();
            ProductCoupons = new HashSet<ProductCoupon>();
        }

        [Key]
        public int ProductId { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [MaxLength(255)]
        public string ?Slug { get; set; }

        [MaxLength(100)]
        public string ?SKU { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        

        [Range(0, double.MaxValue)]
        public decimal? DiscountPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [Range(0, 5)]
        public double StarRating { get; set; }

        [Range(1, int.MaxValue)]
        public int? ReturnDeliveryDay { get; set; }

        public decimal? ImportPrice { get; set; }

        public int ?CategoryId { get; set; }
        public Category Category { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        public int ?ViewCount { get; set; } = 0;
        public int ?SoldCount { get; set; } = 0;

        [MaxLength(160)]
        public string ?MetaDescription { get; set; }

        [MaxLength(255)]
        public string ?MetaKeywords { get; set; }

        public string productType { get; set; } = "Normal"; // flashsale

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationships
        public ICollection<ProductImage> Images { get; set; }
        public ICollection<ProductColor> Colors { get; set; }
        public ICollection<ProductSize> ProductSizes { get; set; }
        public ICollection<ProductCoupon> ProductCoupons { get; set; }

        public ICollection<Review> Reviews { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<FlashSaleItem> FlashSaleItems { get; set; }
        public ICollection<WishListItem> WishlistItems { get; set; }
    }
}
