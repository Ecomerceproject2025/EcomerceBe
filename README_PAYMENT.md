# Payment Integration - MoMo

## 📋 Tổng quan

EcomerceBE tích hợp **MoMo QR Payment** để xử lý thanh toán trực tuyến. Hệ thống hỗ trợ cả **demo mode** (để test) và **production mode** (với real MoMo API).

## 🔄 Payment Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    PAYMENT FLOW                              │
└─────────────────────────────────────────────────────────────┘
1. User chọn MoMo payment trong checkout
   ↓
2. Frontend gọi POST /api/payment/momo/create
   { orderId, amount, orderInfo }
   ↓
3. Backend tạo payment request với MoMo API
   → Generate signature
   → Call MoMo API
   ↓
4. MoMo trả về payment URL và QR code
   ↓
5. Backend trả về cho Frontend:
   { paymentUrl, qrCode, orderId }
   ↓
6. Frontend hiển thị QR code
   ↓
7. User quét QR và thanh toán trên MoMo app
   ↓
8. MoMo gọi callback:
   POST /api/payment/momo/notify (IPN)
   ↓
9. Backend verify signature
   → Update order status
   → Return success
   ↓
10. MoMo redirect user về:
    GET /api/payment/momo/return
    ↓
11. Frontend redirect về order success page
```

## 📁 Files liên quan

### Controllers

- **`Controllers/MoMoPaymentController.cs`**
  - `POST /api/payment/momo/create` - Tạo payment request
  - `POST /api/payment/momo/notify` - MoMo callback (IPN)
  - `GET /api/payment/momo/return` - Return URL sau khi thanh toán

### Services

- **`Service/Payment/IMoMoPaymentService.cs`** - Interface
- **`Service/Payment/MoMoPaymentService.cs`** - Implementation
  - `CreatePaymentRequestAsync(MoMoPaymentRequest request)` - Tạo payment request
  - `VerifyPaymentCallbackAsync(...)` - Verify callback signature

### DTOs

- **`MoMoPaymentRequest`** - Request DTO
- **`MoMoPaymentResponse`** - Response DTO

## ⚙️ Cấu hình

### appsettings.json

```json
{
  "MoMo": {
    "PartnerCode": "MOMO",
    "AccessKey": "F8BBA842ECF85",
    "SecretKey": "K951B6PE1waDMi640xX08PD3vg6EkVlz",
    "ApiEndpoint": "https://test-payment.momo.vn/v2/gateway/api/create",
    "ReturnUrl": "http://localhost:5099/api/payment/momo/return",
    "NotifyUrl": "http://localhost:5099/api/payment/momo/notify"
  }
}
```

### Production Configuration

Thay đổi các giá trị sau cho production:

```json
{
  "MoMo": {
    "PartnerCode": "YOUR_PARTNER_CODE",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "ApiEndpoint": "https://payment.momo.vn/v2/gateway/api/create",
    "ReturnUrl": "https://yourdomain.com/api/payment/momo/return",
    "NotifyUrl": "https://yourdomain.com/api/payment/momo/notify"
  }
}
```

## 🔐 Signature Generation

MoMo yêu cầu signature để verify requests:

```csharp
// Create raw signature string
var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";

// HMAC SHA256
using var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
var signatureBytes = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(rawSignature));
var signature = Convert.ToBase64String(signatureBytes);
```

## 📝 API Endpoints

### Create Payment Request

**Endpoint**: `POST /api/payment/momo/create`

**Authentication**: Required

**Request Body**:
```json
{
  "orderId": "ORDER123",
  "amount": 100000,
  "orderInfo": "Payment for order ORDER123",
  "returnUrl": "http://localhost:3000/payment/success",
  "notifyUrl": "http://localhost:5099/api/payment/momo/notify"
}
```

**Response**:
```json
{
  "paymentUrl": "https://test-payment.momo.vn/...",
  "qrCode": "data:image/png;base64,...",
  "orderId": "ORDER123"
}
```

**Status Codes**:
- `200 OK` - Success
- `400 Bad Request` - Invalid request
- `500 Internal Server Error` - MoMo API error

---

### Payment Callback (IPN)

**Endpoint**: `POST /api/payment/momo/notify`

**Authentication**: Not required (MoMo calls this endpoint)

**Request Body** (from MoMo):
```json
{
  "partnerCode": "MOMO",
  "orderId": "ORDER123",
  "requestId": "REQUEST123",
  "amount": 100000,
  "orderInfo": "Payment for order ORDER123",
  "orderType": "momo_wallet",
  "transId": 1234567890,
  "resultCode": 0,
  "message": "Success",
  "payType": "qr",
  "responseTime": 1234567890,
  "extraData": "",
  "signature": "signature_string"
}
```

**Response**: `200 OK` với body `"success"` hoặc `"failed"`

**Lưu ý**: Endpoint này phải verify signature và update order status.

---

### Return URL

**Endpoint**: `GET /api/payment/momo/return`

**Authentication**: Not required

**Query Parameters**:
- `partnerCode` - Partner code
- `orderId` - Order ID
- `requestId` - Request ID
- `amount` - Amount
- `orderInfo` - Order info
- `orderType` - Order type
- `transId` - Transaction ID
- `resultCode` - Result code (0 = success)
- `message` - Message
- `payType` - Payment type
- `responseTime` - Response time
- `extraData` - Extra data
- `signature` - Signature

**Response**: Redirect to frontend success page

## 🧪 Demo Mode

Hiện tại hệ thống hỗ trợ **demo mode** để test mà không cần real MoMo API:

```csharp
// Trong MoMoPaymentService.cs
public async Task<MoMoPaymentResponse> CreatePaymentRequestAsync(MoMoPaymentRequest request)
{
    // Demo mode: Return mock response
    if (IsDemoMode())
    {
        return new MoMoPaymentResponse
        {
            PaymentUrl = "https://test-payment.momo.vn/demo",
            QrCode = GenerateMockQrCode(),
            OrderId = request.OrderId
        };
    }
    
    // Production mode: Call real MoMo API
    // ...
}
```

## 🔍 Testing

### Test với Swagger

1. Mở Swagger UI: `http://localhost:5099/swagger`
2. Authorize với JWT token
3. Test `POST /api/payment/momo/create`
4. Kiểm tra response có `paymentUrl` và `qrCode`

### Test Callback

Sử dụng tool như Postman để simulate MoMo callback:

```bash
curl -X POST http://localhost:5099/api/payment/momo/notify \
  -H "Content-Type: application/json" \
  -d '{
    "partnerCode": "MOMO",
    "orderId": "ORDER123",
    "amount": 100000,
    "resultCode": 0,
    "signature": "test_signature"
  }'
```

## 🐛 Troubleshooting

### Lỗi: "Invalid signature"

- Kiểm tra `SecretKey` trong `appsettings.json` đúng
- Đảm bảo signature được generate đúng format
- Kiểm tra raw signature string có đúng thứ tự parameters

### Lỗi: "MoMo API error"

- Kiểm tra `ApiEndpoint` đúng (test vs production)
- Kiểm tra `PartnerCode`, `AccessKey`, `SecretKey` đúng
- Kiểm tra network connectivity đến MoMo API

### Lỗi: "Order not found"

- Đảm bảo order tồn tại trong database
- Kiểm tra `orderId` trong request đúng format

## 📚 Related Documentation

- [Frontend Payment Integration](../E_Com_FE/Ecom_app/PAYMENT_INTEGRATION.md)
- [API Controllers](./README_API_CONTROLLERS.md)
- [Services](./README_SERVICES.md)

---

**Last Updated**: 2024-12-17

