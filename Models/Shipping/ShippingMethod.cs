using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class ShippingMethod
    {
        public int ShippingMethodId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // "Standard", "Express", "Same Day", etc.
        
        [MaxLength(255)]
        public string? Description { get; set; }
        
        [Required]
        public decimal Price { get; set; } // Price in VND
        
        [MaxLength(50)]
        public string? EstimatedDays { get; set; } // "3-5 days", "1-2 days", etc.
        
        public bool IsActive { get; set; } = true;
        
        public int DisplayOrder { get; set; } = 0; // For sorting
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

