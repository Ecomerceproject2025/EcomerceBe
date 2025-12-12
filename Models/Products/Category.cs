using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        [MaxLength(255)] public string Name { get; set; }
        public int? ParentId { get; set; }
        public Category Parent { get; set; }
        public ICollection<Category> Children { get; set; }
        public ICollection<Product> Products { get; set; }
        public ICollection<CategorySize> CategorySizes { get; set; }
        public ICollection<Brand> Brands { get; set; }  
    }
}
