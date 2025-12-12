
    
        public sealed class FlashSaleCreateDTO
        {
    public string Name { get; set; }
    public string Day { get; set; }
    public string Hours { get; set; }
    public string Minutes { get; set; }
    public string Seconds { get; set; }
}


public class FlashSaleUpdateDTO
{
    public string Name { get; set; }
    public int Day { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public int Seconds { get; set; }
}

public sealed class AddProductToFlashSaleDTO
    {
        public string productType { get; set; }
        public int ProductId { get; set; }
        public decimal FlashPrice { get; set; }
        public int FlashStock { get; set; }
    }

