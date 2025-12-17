using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // price at time of order
        public string? Size { get; set; }
        public string? Color { get; set; }

        public Order Order { get; set; }
        public Product Product { get; set; }
      
    }
}
