using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Address
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }
        public string Phone { get; set; } = string.Empty;
        [MaxLength(255)] public string Street { get; set; }
        [MaxLength(255)] public string Province { get; set; }
        [MaxLength(255)] public string ?Apartment { get; set; }
        [MaxLength(255)] public string City { get; set; }
        [MaxLength(255)] public string PostalCode { get; set; }
        [MaxLength(255)] public string Country { get; set; }
        public bool IsDefault { get; set; }

        // Navigation
        public User User { get; set; }
        public ICollection<Order> Orders { get; set; } // orders can reference an address
    }
}
