using System.ComponentModel.DataAnnotations;
using System.Net;

namespace EcomerceBE.Models
{
   
        public class User
        {
            public int Id { get; set; }
            [MaxLength(255)] public string Name { get; set; }
            [MaxLength(255)] public string Email { get; set; }
           
        [MaxLength(255)] public string PasswordHash { get; set; }
          public string Role { get; set; } = "User"; // Default role is "User"
        public string status { get; set; } = "inActive";
        public string Avatar { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // Navigation
            public ICollection<Address> Addresses { get; set; }
            public ICollection<Cart> Carts { get; set; }
            public ICollection<Order> Orders { get; set; }
            public ICollection<Review> Reviews { get; set; }
            public ICollection<WishList> Wishlists { get; set; }
            public ICollection<Contact> Contacts { get; set; }
            public Wallet Wallet { get; set; }
        
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
         public  string ? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationExpiry { get; set; }

        public ICollection<UserCoupon> UserCoupons { get; set; }

    }
}
