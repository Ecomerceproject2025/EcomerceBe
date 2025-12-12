namespace EcomerceBE.Models
{
    public class WishListItem
    {
        public int WishlistItemId { get; set; }
        public int WishlistId { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public WishList Wishlist { get; set; }
        public Product Product { get; set; }
    }
}
