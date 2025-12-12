namespace EcomerceBE.Models
{
    public class FlashSaleItem
    {
        public int FlashSaleItemId { get; set; }
        public int FlashSaleId { get; set; }
        public Product Product { get; set; }
        public int ProductId { get; set; }
        public FlashSale FlashSale { get; set; }
        public decimal DiscountPrice { get; set; }
        public int saleQuantity { get; set; }
        public int Sold { get; set; }
    }
}
