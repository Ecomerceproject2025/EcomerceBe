using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.Models
{
    public class Contact
    {
        public int ContactId { get; set; }
        public int UserId { get; set; }
        [MaxLength(255)] public string Subject { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
    }
}
