namespace EcomerceBE.Models
{
    public class Brand
    {
       public int  BrandId { get; set; }
       public string Name { get; set; }

      public  string code { get; set; }
      public  string Description { get; set; }
       public string Image { get; set; }
      public  Category Categories { get; set; }
        public int CategoryId { get; set; }
    }
}
