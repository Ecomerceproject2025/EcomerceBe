using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcomerceBE.Service.Payment
{
    public class MoMoPaymentService : IMoMoPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MoMoPaymentService> _logger;
        private readonly string _partnerCode;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _apiEndpoint;
        private readonly string _returnUrl;
        private readonly string _notifyUrl;

        public MoMoPaymentService(IConfiguration configuration, ILogger<MoMoPaymentService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _partnerCode = _configuration["MoMo:PartnerCode"] ?? "";
            _accessKey = _configuration["MoMo:AccessKey"] ?? "";
            _secretKey = _configuration["MoMo:SecretKey"] ?? "";
            _apiEndpoint = _configuration["MoMo:ApiEndpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
            _returnUrl = _configuration["MoMo:ReturnUrl"] ?? "";
            _notifyUrl = _configuration["MoMo:NotifyUrl"] ?? "";

            if (string.IsNullOrEmpty(_partnerCode) || string.IsNullOrEmpty(_accessKey) || string.IsNullOrEmpty(_secretKey))
            {
                _logger.LogWarning("MoMo payment credentials are not configured. Payment will not work.");
            }
        }

        public async Task<MoMoPaymentResponse> CreatePaymentRequestAsync(MoMoPaymentRequest request)
        {
            try
            {
                // Generate requestId
                var requestId = Guid.NewGuid().ToString();
                var orderId = request.OrderId;
                var orderInfo = request.OrderInfo;
                var amount = request.Amount;
                var returnUrl = string.IsNullOrEmpty(request.ReturnUrl) ? _returnUrl : request.ReturnUrl;
                var notifyUrl = string.IsNullOrEmpty(request.NotifyUrl) ? _notifyUrl : request.NotifyUrl;
                var requestType = request.RequestType;

                // Create raw signature
                var rawSignature = $"accessKey={_accessKey}&amount={amount}&extraData=&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={_partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
                var signature = ComputeHmacSha256(rawSignature, _secretKey);

                // Create request body
                var requestBody = new
                {
                    partnerCode = _partnerCode,
                    partnerName = "Test",
                    storeId = "MomoTestStore",
                    requestId = requestId,
                    amount = amount,
                    orderId = orderId,
                    orderInfo = orderInfo,
                    redirectUrl = returnUrl,
                    ipnUrl = notifyUrl,
                    requestType = requestType,
                    extraData = "",
                    signature = signature,
                    lang = "vi"
                };

                var json = JsonSerializer.Serialize(requestBody);
                _logger.LogInformation("MoMo Payment Request: {Json}", json);

                // Send request to MoMo API
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(_apiEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("MoMo Payment Response: {Response}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"MoMo API error: {response.StatusCode} - {responseContent}");
                }

                var momoResponse = JsonSerializer.Deserialize<MoMoApiResponse>(responseContent);
                if (momoResponse == null || momoResponse.ResultCode != 0)
                {
                    throw new Exception($"MoMo payment failed: {momoResponse?.Message ?? "Unknown error"}");
                }

                // Generate QR code URL (MoMo provides payUrl which can be converted to QR)
                return new MoMoPaymentResponse
                {
                    PayUrl = momoResponse.PayUrl ?? "",
                    QrCodeUrl = momoResponse.PayUrl ?? "", // PayUrl can be used to generate QR code
                    OrderId = orderId,
                    Amount = amount,
                    RequestId = requestId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MoMo payment request");
                throw;
            }
        }

        public bool VerifyPaymentCallback(MoMoCallbackData callbackData)
        {
            try
            {
                // Create raw signature for verification
                var rawSignature = $"accessKey={_accessKey}&amount={callbackData.Amount}&extraData={callbackData.ExtraData}&message={callbackData.Message}&orderId={callbackData.OrderId}&orderInfo=&partnerCode={callbackData.PartnerCode}&payType={callbackData.PayType}&requestId={callbackData.RequestId}&responseTime={callbackData.ResponseTime}&resultCode={callbackData.ResultCode}&transId={callbackData.TransId}";
                var expectedSignature = ComputeHmacSha256(rawSignature, _secretKey);

                // Verify signature
                if (!string.Equals(expectedSignature, callbackData.Signature, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("MoMo callback signature mismatch. Expected: {Expected}, Received: {Received}", expectedSignature, callbackData.Signature);
                    return false;
                }

                // Verify result code (0 = success)
                if (callbackData.ResultCode != 0)
                {
                    _logger.LogWarning("MoMo payment failed. ResultCode: {ResultCode}, Message: {Message}", callbackData.ResultCode, callbackData.Message);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying MoMo payment callback");
                return false;
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private class MoMoApiResponse
        {
            public int ResultCode { get; set; }
            public string Message { get; set; } = string.Empty;
            public string PayUrl { get; set; } = string.Empty;
            public string QrCodeUrl { get; set; } = string.Empty;
            public string RequestId { get; set; } = string.Empty;
        }
    }
}

