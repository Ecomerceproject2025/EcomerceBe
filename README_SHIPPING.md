# Shipping Module

## 📋 Tổng quan

Module Shipping quản lý phương thức vận chuyển và tài khoản shipper. Hệ thống hỗ trợ nhiều shipping methods và tracking đơn hàng qua các shipper accounts.

## 🗄️ Database Schema

### ShippingMethods Table

| Column | Type | Description |
|--------|------|-------------|
| ShippingMethodId | INT (PK) | Shipping method ID |
| Name | VARCHAR | Tên phương thức vận chuyển |
| Description | TEXT | Mô tả |
| Cost | DECIMAL(18,2) | Chi phí vận chuyển |
| EstimatedDays | INT | Số ngày ước tính |
| IsActive | BOOLEAN | Trạng thái hoạt động |
| CreatedAt | DATETIME | Created timestamp |

### Shippings Table

| Column | Type | Description |
|--------|------|-------------|
| ShippingId | INT (PK) | Shipping ID |
| OrderId | INT (FK) | Order ID |
| ShippingMethodId | INT (FK) | Shipping method ID |
| ShipperId | INT (FK, nullable) | Shipper user ID |
| Status | VARCHAR | Shipping status |
| TrackingNumber | VARCHAR | Tracking number |
| ShippedAt | DATETIME | Shipped timestamp |
| DeliveredAt | DATETIME | Delivered timestamp |
| CreatedAt | DATETIME | Created timestamp |

## 📁 Files liên quan

### Controllers

- **`Controllers/ShippingController.cs`**
  - `GET /api/shipping` - Lấy danh sách shippings (Admin)
  - `GET /api/shipping/{id}` - Lấy chi tiết shipping (Admin)
  - `POST /api/shipping` - Tạo shipping mới (Admin)
  - `PUT /api/shipping/{id}` - Cập nhật shipping (Admin)
  - `DELETE /api/shipping/{id}` - Xóa shipping (Admin)

- **`Controllers/ShippingMethodController.cs`**
  - `GET /api/shippingmethod` - Lấy danh sách shipping methods
  - `GET /api/shippingmethod/{id}` - Lấy chi tiết shipping method
  - `POST /api/shippingmethod` - Tạo shipping method mới (Admin)
  - `PUT /api/shippingmethod/{id}` - Cập nhật shipping method (Admin)
  - `DELETE /api/shippingmethod/{id}` - Xóa shipping method (Admin)

### Models

- **`Models/Shipping/ShippingMethod.cs`**
- **`Models/Shipping/Shipping.cs`**

## 🔄 Shipping Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    SHIPPING FLOW                             │
└─────────────────────────────────────────────────────────────┘
1. Admin tạo Shipping Method
   POST /api/shippingmethod
   { name, cost, estimatedDays }
   ↓
2. User checkout và chọn Shipping Method
   POST /api/checkout
   { shippingMethodId, ... }
   ↓
3. Order được tạo với ShippingMethodId
   ↓
4. Admin tạo Shipping record
   POST /api/shipping
   { orderId, shippingMethodId, shipperId }
   ↓
5. Shipper nhận đơn và cập nhật status
   PUT /api/shipping/{id}
   { status: "shipped", trackingNumber }
   ↓
6. Shipper cập nhật khi giao hàng thành công
   PUT /api/shipping/{id}
   { status: "delivered", deliveredAt }
```

## 📝 API Endpoints

### Shipping Methods

#### Get All Shipping Methods

**Endpoint**: `GET /api/shippingmethod`

**Authentication**: Not required (public)

**Response**:
```json
[
  {
    "shippingMethodId": 1,
    "name": "Standard Shipping",
    "description": "5-7 business days",
    "cost": 30000,
    "estimatedDays": 5,
    "isActive": true
  }
]
```

---

#### Create Shipping Method

**Endpoint**: `POST /api/shippingmethod`

**Authentication**: Required (Admin)

**Request Body**:
```json
{
  "name": "Express Shipping",
  "description": "2-3 business days",
  "cost": 50000,
  "estimatedDays": 2,
  "isActive": true
}
```

**Response**: `200 OK` với shipping method object

---

### Shippings

#### Get All Shippings

**Endpoint**: `GET /api/shipping`

**Authentication**: Required (Admin)

**Query Parameters**:
- `orderId` (int, optional) - Filter by order ID
- `status` (string, optional) - Filter by status
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 20) - Page size

**Response**:
```json
{
  "data": [
    {
      "shippingId": 1,
      "orderId": 123,
      "shippingMethodId": 1,
      "shipperId": 5,
      "status": "shipped",
      "trackingNumber": "TRACK123",
      "shippedAt": "2024-12-17T10:00:00Z",
      "createdAt": "2024-12-17T09:00:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20
}
```

---

#### Create Shipping

**Endpoint**: `POST /api/shipping`

**Authentication**: Required (Admin)

**Request Body**:
```json
{
  "orderId": 123,
  "shippingMethodId": 1,
  "shipperId": 5,
  "status": "pending"
}
```

**Response**: `200 OK` với shipping object

---

#### Update Shipping

**Endpoint**: `PUT /api/shipping/{id}`

**Authentication**: Required (Admin)

**Request Body**:
```json
{
  "status": "shipped",
  "trackingNumber": "TRACK123",
  "shippedAt": "2024-12-17T10:00:00Z"
}
```

**Response**: `200 OK` với updated shipping object

---

## 🔐 Shipper Management

Shipper accounts là các User với role "Shipper" hoặc có thể là separate entity. Hiện tại hệ thống sử dụng User ID để reference shipper.

### Assign Shipper to Shipping

```csharp
var shipping = new Shipping
{
    OrderId = orderId,
    ShippingMethodId = shippingMethodId,
    ShipperId = shipperUserId, // User ID của shipper
    Status = "pending"
};
_context.Shippings.Add(shipping);
await _context.SaveChangesAsync();
```

## 📊 Shipping Statuses

Các status có thể có:
- `pending` - Chờ xử lý
- `preparing` - Đang chuẩn bị
- `shipped` - Đã gửi hàng
- `in_transit` - Đang vận chuyển
- `delivered` - Đã giao hàng
- `cancelled` - Đã hủy
- `returned` - Đã trả hàng

## 🔍 Integration với Orders

Shipping được link với Order qua `OrderId`:

```csharp
var order = await _context.Orders
    .Include(o => o.ShippingMethod)
    .FirstOrDefaultAsync(o => o.OrderId == orderId);

var shipping = await _context.Shippings
    .Include(s => s.Order)
        .ThenInclude(o => o.User)
    .Include(s => s.Order)
        .ThenInclude(o => o.Address)
    .FirstOrDefaultAsync(s => s.OrderId == orderId);
```

## 🐛 Troubleshooting

### Lỗi: "Order not found"

- Đảm bảo order tồn tại trước khi tạo shipping
- Kiểm tra `orderId` đúng format

### Lỗi: "Shipping method not found"

- Đảm bảo shipping method tồn tại và `isActive = true`
- Kiểm tra `shippingMethodId` đúng

### Lỗi: "Shipper not found"

- Đảm bảo shipper user tồn tại
- Kiểm tra `shipperId` đúng format

## 📚 Related Documentation

- [API Controllers](./README_API_CONTROLLERS.md) - Shipping endpoints
- [Database](./README_DATABASE.md) - Shipping models
- [Orders](./README_API_CONTROLLERS.md#orders--checkout) - Order integration

---

**Last Updated**: 2024-12-17

