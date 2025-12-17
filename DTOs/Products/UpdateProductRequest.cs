namespace EcomerceBE.DTOs
{
    public class UpdateProductRequest
    {
        public string Id { get; set; } // FE gửi string "215"
        public string Title { get; set; } // Map sang Product.Name
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal ImportPrice { get; set; }

        public string ? productType { get; set; } 
        // Ảnh không có SalePrice, nếu FE không gửi thì mặc định null hoặc 0
        public decimal ? SalePrice { get; set; }
        public int ReturnDeliveryDay { get; set; }
        public int ?CategoryId { get; set; } // FE gửi số (number)
        public string Category { get; set; } // Tên category, thường chỉ để hiển thị, không lưu
        public string Status { get; set; }   // "draft"
        public string HeroImage { get; set; } // Ảnh đại diện chính

        // Map sang Product.Images
        public List<string> ProductImages { get; set; }

        // Map sang Product.ProductCoupons
        public List<object> ListCoupon { get; set; }

        // Map sang Product.Variants (Logic Size/Color)
        public List<ProductVariantDto> Sizes { get; set; }
        public int ? saleQuantity { get; set; }
    }

    // Class con cho Variants (đảm bảo FE gửi đúng cấu trúc này trong mảng 'sizes')
    public class ProductVariantDto
    {
        public VariantSizeDto size { get; set; }
        public string colorHex { get; set; }
        public int quantity { get; set; }
        public int? saleQuantity { get; set; }      // Sale quantity for flash sale (optional)
    }

    public class VariantSizeDto
    {
        public string type { get; set; } // "select" hoặc "custom"
        public string value { get; set; }
    }
}
