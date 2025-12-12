using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class FlashSale
    {
        public FlashSale()
        {
            FlashSaleItems = new HashSet<FlashSaleItem>();
        }

        [Key]
        public int FlashSaleId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public ICollection<FlashSaleItem> FlashSaleItems { get; set; }
    }
}
