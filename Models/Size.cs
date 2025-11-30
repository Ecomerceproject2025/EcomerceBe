using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Size
    {
        public int SizeId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public string? Description { get; set; }

        public ICollection<CategorySize> CategorySizes { get; set; }
        public ICollection<ProductSize> ProductSizes { get; set; }
    }
}
