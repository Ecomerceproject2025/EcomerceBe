namespace EcomerceBE.Service.Payment
{
    public interface IMoMoPaymentService
    {
        Task<MoMoPaymentResponse> CreatePaymentRequestAsync(MoMoPaymentRequest request);
        bool VerifyPaymentCallback(MoMoCallbackData callbackData);
    }

    public class MoMoPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
        public string RequestType { get; set; } = "captureWallet"; // captureWallet for QR payment
    }

    public class MoMoPaymentResponse
    {
        public string PayUrl { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string RequestId { get; set; } = string.Empty;
    }

    public class MoMoCallbackData
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public long TransId { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}

