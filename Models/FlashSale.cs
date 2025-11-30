using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class FlashSale
    {

        public int FlashSaleId { get; set; }
        [MaxLength(255)] public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public ICollection<FlashSaleItem> FlashSaleItems { get; set; }
    }
}
