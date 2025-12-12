namespace EcomerceBE.DTOs
{
    public class OtpVerifyRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
         public string NewPassword { get; set; }
    }
}
    