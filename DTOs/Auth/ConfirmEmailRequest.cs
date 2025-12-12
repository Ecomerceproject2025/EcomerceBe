namespace EcomerceBE.DTOs
{
    public class ConfirmEmailRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
    }
}
