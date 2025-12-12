namespace EcomerceBE.Models
{
    public class CategorySize
    {
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int SizeId { get; set; }
        public Size Size { get; set; }
    }
}
