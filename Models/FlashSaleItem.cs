namespace EcomerceBE.Models
{
    public class FlashSaleItem
    {
        public int FlashSaleItemId { get; set; }
        public int FlashSaleId { get; set; }
        public int ProductId { get; set; }
        public decimal DiscountPrice { get; set; }

        public FlashSale FlashSale { get; set; }
        public Product Product { get; set; }
    }
}
