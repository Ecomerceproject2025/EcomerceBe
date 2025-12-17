using EcomerceBE.Models;

public class ProductSize
{
    public int ProductSizeId { get; set; }            // PK (Identity)
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? SizeId { get; set; }                  // null nếu custom
    public Size? Size { get; set; }

    public string? CustomValue { get; set; }          // text cho custom size

    public ICollection<ProductColor> ProductColors { get; set; } = new List<ProductColor>();
}

public class ProductColor
{
    public int ProductColorId { get; set; }           // PK (Identity)
    public string ColorCode { get; set; } = string.Empty; // "#RRGGBB"
    public int ProductSizeId { get; set; }            // FK tới ProductSize
    public ProductSize ProductSize { get; set; } = null!;

    public int Quantity { get; set; }
    public int? SaleQuantity { get; set; }            // Sale quantity for flash sale (optional)
}
