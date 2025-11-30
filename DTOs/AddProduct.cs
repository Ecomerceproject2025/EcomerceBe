using EcomerceBE.Models;

public sealed class AddProduct
{
    public List<string> Images { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ImportPrice { get; set; }
    public decimal Price { get; set; }
    public decimal SalePrice { get; set; }
    public int? ReturnDeliveryDay { get; set; }
   
    public int ?CategoryId { get; set; }
    public List<VariantVm> Variants { get; set; } = new();
    public List<Coupon>? coupons { get; set; } = new();

    
}

public sealed class VariantVm
{
    public SizeVm size { get; set; } = new();      // { type, value }
    public string colorHex { get; set; } = "#000000";
    public int quantity { get; set; }
}

public sealed class SizeVm
{
    public string type { get; set; } = "select";   // "select" | "custom"
    public string value { get; set; } = string.Empty;
}
