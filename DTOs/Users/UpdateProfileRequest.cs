namespace EcomerceBE.DTOs
{
    public class UpdateProfileRequest
    {
        public string? Avatar { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}

