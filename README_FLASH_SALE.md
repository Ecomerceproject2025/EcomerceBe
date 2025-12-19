# Flash Sale Module

## 📋 Tổng quan

Module Flash Sale quản lý các sự kiện flash sale với giá giảm và thời gian giới hạn. Hệ thống tự động cleanup các flash sales đã hết hạn và quản lý inventory.

## 🗄️ Database Schema

### FlashSales Table

| Column | Type | Description |
|--------|------|-------------|
| FlashSaleId | INT (PK) | Flash sale ID |
| StartTime | DATETIME | Thời gian bắt đầu |
| EndTime | DATETIME | Thời gian kết thúc |
| IsActive | BOOLEAN | Trạng thái hoạt động |
| CreatedAt | DATETIME | Created timestamp |

### FlashSaleItems Table

| Column | Type | Description |
|--------|------|-------------|
| FlashSaleItemId | INT (PK) | Flash sale item ID |
| FlashSaleId | INT (FK) | Flash sale ID |
| ProductId | INT (FK) | Product ID |
| DiscountPrice | DECIMAL(18,2) | Giá giảm |
| Quantity | INT | Số lượng sản phẩm |
| SoldQuantity | INT | Số lượng đã bán |
| CreatedAt | DATETIME | Created timestamp |

## 📁 Files liên quan

### Controllers

- **`Controllers/FlashSaleController.cs`**
  - `GET /api/flashsale/GetAllFlashSales` - Lấy tất cả flash sales
  - `PUT /api/flashsale/SetTimeFlashSale` - Tạo/cập nhật flash sale (Admin)
  - `DELETE /api/flashsale/{id}` - Xóa flash sale (Admin)
  - `GET /api/flashsale/{id}/items` - Lấy items của flash sale

### Services

- **`Service/flashSale/IFlashSaleService.cs`** - Interface
- **`Service/flashSale/FlashSaleService.cs`** - Implementation
  - `GetAllFlashSalesWithItemsAsync()` - Lấy tất cả flash sales với items
  - `CreateOrUpdateFlashSaleAsync(int? id, FlashSaleCreateDTO dto)` - Tạo/cập nhật
  - `DeleteFlashSaleAsync(int id)` - Xóa flash sale
  - `CleanupExpiredFlashSalesAsync()` - Cleanup flash sales đã hết hạn

### Models

- **`Models/FlashSales/FlashSale.cs`**
- **`Models/FlashSales/FlashSaleItem.cs`**

### DTOs

- **`DTOs/FlashSale/FlashSaleCreateDTO.cs`**

### Background Services

- **`Background/FlashSaleCleanupService.cs`** - Tự động cleanup mỗi 5 phút

## 🔄 Flash Sale Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    FLASH SALE FLOW                           │
└─────────────────────────────────────────────────────────────┘
1. Admin tạo Flash Sale
   PUT /api/flashsale/SetTimeFlashSale
   { startTime, endTime, items: [{ productId, discountPrice, quantity }] }
   ↓
2. Flash Sale được lưu vào DB
   ↓
3. User xem Flash Sale
   GET /api/flashsale/GetAllFlashSales
   → Chỉ trả về flash sales đang active và chưa hết hạn
   ↓
4. User mua sản phẩm trong Flash Sale
   → Giá được tính theo discountPrice
   → SoldQuantity được tăng lên
   ↓
5. Background Service cleanup mỗi 5 phút
   → Xóa flash sales đã hết hạn (EndTime < Now)
   → Hoặc set IsActive = false
```

## 📝 API Endpoints

### Get All Flash Sales

**Endpoint**: `GET /api/flashsale/GetAllFlashSales`

**Authentication**: Not required (public)

**Response**:
```json
[
  {
    "flashSaleId": 1,
    "startTime": "2024-12-17T00:00:00Z",
    "endTime": "2024-12-17T23:59:59Z",
    "isActive": true,
    "items": [
      {
        "flashSaleItemId": 1,
        "productId": 123,
        "product": {
          "productId": 123,
          "name": "Product Name",
          "price": 100000,
          "heroImage": "https://..."
        },
        "discountPrice": 80000,
        "quantity": 100,
        "soldQuantity": 25
      }
    ]
  }
]
```

**Lưu ý**: Endpoint này tự động cleanup expired flash sales trước khi trả về.

---

### Create/Update Flash Sale

**Endpoint**: `PUT /api/flashsale/SetTimeFlashSale?id={id}`

**Authentication**: Required (Admin)

**Query Parameters**:
- `id` (int, optional) - Flash sale ID để update (nếu không có thì tạo mới)

**Request Body**:
```json
{
  "startTime": "2024-12-17T00:00:00Z",
  "endTime": "2024-12-17T23:59:59Z",
  "items": [
    {
      "productId": 123,
      "discountPrice": 80000,
      "quantity": 100
    },
    {
      "productId": 456,
      "discountPrice": 150000,
      "quantity": 50
    }
  ]
}
```

**Response**: `200 OK` với flash sale object

**Status Codes**:
- `200 OK` - Success
- `400 Bad Request` - Invalid data (e.g., endTime < startTime)
- `404 Not Found` - Flash sale not found (khi update)

---

### Delete Flash Sale

**Endpoint**: `DELETE /api/flashsale/{id}`

**Authentication**: Required (Admin)

**Response**: `200 OK` với message "Flash sale deleted successfully"

---

### Get Flash Sale Items

**Endpoint**: `GET /api/flashsale/{id}/items`

**Authentication**: Not required (public)

**Response**: Array of flash sale items

## 🔄 Background Cleanup

### FlashSaleCleanupService

**Location**: `Background/FlashSaleCleanupService.cs`

**Chức năng**: Tự động cleanup flash sales đã hết hạn mỗi 5 phút

**Implementation**:
```csharp
private async Task RunCleanupAsync()
{
    using var scope = _serviceProvider.CreateScope();
    var flashSaleService = scope.ServiceProvider.GetRequiredService<IFlashSaleService>();
    await flashSaleService.CleanupExpiredFlashSalesAsync();
}
```

**Registration**:
```csharp
builder.Services.AddHostedService<FlashSaleCleanupService>();
```

Xem chi tiết tại [README_BACKGROUND_SERVICES.md](./README_BACKGROUND_SERVICES.md).

## 📊 Inventory Management

Khi user mua sản phẩm trong flash sale:

```csharp
var flashSaleItem = await _context.FlashSaleItems
    .FirstOrDefaultAsync(fsi => fsi.FlashSaleId == flashSaleId && fsi.ProductId == productId);

if (flashSaleItem == null || flashSaleItem.SoldQuantity >= flashSaleItem.Quantity)
{
    return BadRequest("Flash sale item is sold out");
}

// Update sold quantity
flashSaleItem.SoldQuantity += quantity;
await _context.SaveChangesAsync();
```

## 🔍 Integration với Products

Flash sale items được link với Products:

```csharp
var flashSale = await _context.FlashSales
    .Include(fs => fs.FlashSaleItems)
        .ThenInclude(fsi => fsi.Product)
            .ThenInclude(p => p.Images)
    .FirstOrDefaultAsync(fs => fs.FlashSaleId == id);
```

## 🐛 Troubleshooting

### Lỗi: "Flash sale with id not found"

- Kiểm tra flash sale ID đúng
- Đảm bảo flash sale chưa bị xóa

### Lỗi: "End time must be after start time"

- Kiểm tra `endTime` > `startTime`
- Đảm bảo timezone đúng (UTC)

### Lỗi: "Flash sale item is sold out"

- Kiểm tra `SoldQuantity < Quantity`
- Có thể cần tăng `Quantity` hoặc reset `SoldQuantity`

### Flash Sale không hiển thị

- Kiểm tra `IsActive = true`
- Kiểm tra `EndTime > Now`
- Kiểm tra background cleanup service đang chạy

## 📚 Related Documentation

- [API Controllers](./README_API_CONTROLLERS.md) - Flash sale endpoints
- [Database](./README_DATABASE.md) - Flash sale models
- [Background Services](./README_BACKGROUND_SERVICES.md) - Cleanup service details
- [Services](./README_SERVICES.md) - Flash sale service details

---

**Last Updated**: 2024-12-17

