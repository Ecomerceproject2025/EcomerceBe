namespace EcomerceBE.Models
{
    public class WishList
    {
        public int WishlistId { get; set; }
        public int UserId { get; set; }

        public User User { get; set; }
        public ICollection<WishListItem> WishlistItems { get; set; }
    }
}
