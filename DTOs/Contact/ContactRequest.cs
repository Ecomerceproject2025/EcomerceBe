using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.DTOs.Contact
{
    public class ContactRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; } = string.Empty;
    }
}

