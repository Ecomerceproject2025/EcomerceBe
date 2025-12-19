# API Controllers Overview

## 📋 Tổng quan

EcomerceBE có nhiều API controllers được tổ chức theo domain. Tất cả controllers sử dụng `[ApiController]` attribute và route prefix `/api/[controller]` hoặc `/api/[custom-route]`.

## 🔐 Authentication & Authorization

### AuthController
**Route**: `/api/auth`

**Endpoints**:
- `POST /api/auth/register` - Đăng ký tài khoản
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/refresh-token` - Refresh access token
- `POST /api/auth/google-login` - Google OAuth login
- `POST /api/auth/send-otp` - Gửi OTP qua email
- `POST /api/auth/verify-otp` - Verify OTP và reset password
- `POST /api/auth/sendEmailVerify` - Gửi email verification
- `POST /api/auth/verification/confirm` - Xác nhận email
- `POST /api/auth/change_profile` - Cập nhật profile

**Authentication**: Một số endpoints yêu cầu authentication, một số không.

Xem chi tiết tại [README_AUTHENTICATION.md](./README_AUTHENTICATION.md).

---

## 🛍️ Products & Catalog

### ProductController
**Route**: `/api/product`

**Endpoints**:
- `GET /api/product/category` - Lấy tất cả categories
- `GET /api/product/category/{categoryId}/sizes` - Lấy sizes theo category
- `GET /api/product` - Lấy danh sách products (với filters, pagination)
- `GET /api/product/{id}` - Lấy chi tiết product
- `POST /api/product` - Tạo product mới (Admin)
- `PUT /api/product/{id}` - Cập nhật product (Admin)
- `DELETE /api/product/{id}` - Xóa product (Admin)
- `POST /api/product/import` - Import products từ Excel (Admin)
- `GET /api/product/export` - Export products ra Excel (Admin)

**Authentication**: Một số endpoints yêu cầu Admin role.

---

### ProductViewController
**Route**: `/api/productview`

**Endpoints**:
- `GET /api/productview` - Lấy products với view count
- `GET /api/productview/{id}` - Lấy product detail với view tracking
- `POST /api/productview/{id}/view` - Track product view

**Authentication**: Không yêu cầu (public endpoints).

---

### ReviewController
**Route**: `/api/review`

**Endpoints**:
- `GET /api/review/product/{productId}` - Lấy reviews của product
- `POST /api/review` - Tạo review mới (Authenticated)
- `PUT /api/review/{id}` - Cập nhật review (Owner)
- `DELETE /api/review/{id}` - Xóa review (Owner/Admin)
- `POST /api/review/{id}/reply` - Reply review (Authenticated)
- `GET /api/review/{id}/replies` - Lấy replies của review

**Authentication**: Một số endpoints yêu cầu authentication.

---

## 🛒 Orders & Checkout

### CheckOutController
**Route**: `/api/checkout`

**Endpoints**:
- `POST /api/checkout` - Tạo order từ cart (Authenticated)
- `POST /api/checkout/guest` - Tạo order cho guest user
- `GET /api/checkout/validate-coupon` - Validate coupon code
- `GET /api/checkout/shipping-methods` - Lấy shipping methods

**Authentication**: Một số endpoints yêu cầu authentication.

---

### OrderController
**Route**: `/api/orders`

**Endpoints**:
- `GET /api/orders` - Lấy danh sách orders (Admin)
- `GET /api/orders/{id}` - Lấy chi tiết order (Admin)
- `PUT /api/orders/{id}/status` - Cập nhật order status (Admin)
- `GET /api/orders/customer-locations` - Thống kê vị trí khách hàng (Admin)
- `GET /api/orders/customer-regions` - Thống kê theo province/city (Admin)

**Authentication**: Tất cả endpoints yêu cầu Admin role.

---

## 🚚 Shipping

### ShippingController
**Route**: `/api/shipping`

**Endpoints**:
- `GET /api/shipping` - Lấy danh sách shippings (Admin)
- `GET /api/shipping/{id}` - Lấy chi tiết shipping (Admin)
- `POST /api/shipping` - Tạo shipping mới (Admin)
- `PUT /api/shipping/{id}` - Cập nhật shipping (Admin)
- `DELETE /api/shipping/{id}` - Xóa shipping (Admin)

**Authentication**: Tất cả endpoints yêu cầu Admin role.

---

### ShippingMethodController
**Route**: `/api/shippingmethod`

**Endpoints**:
- `GET /api/shippingmethod` - Lấy danh sách shipping methods
- `GET /api/shippingmethod/{id}` - Lấy chi tiết shipping method
- `POST /api/shippingmethod` - Tạo shipping method mới (Admin)
- `PUT /api/shippingmethod/{id}` - Cập nhật shipping method (Admin)
- `DELETE /api/shippingmethod/{id}` - Xóa shipping method (Admin)

**Authentication**: Một số endpoints yêu cầu Admin role.

Xem chi tiết tại [README_SHIPPING.md](./README_SHIPPING.md).

---

## 💰 Payment

### MoMoPaymentController
**Route**: `/api/payment/momo`

**Endpoints**:
- `POST /api/payment/momo/create` - Tạo payment request
- `POST /api/payment/momo/notify` - MoMo callback (IPN)
- `GET /api/payment/momo/return` - Return URL sau khi thanh toán

**Authentication**: Một số endpoints yêu cầu authentication.

Xem chi tiết tại [README_PAYMENT.md](./README_PAYMENT.md).

---

## 🎯 Flash Sale

### FlashSaleController
**Route**: `/api/flashsale`

**Endpoints**:
- `GET /api/flashsale/GetAllFlashSales` - Lấy tất cả flash sales
- `PUT /api/flashsale/SetTimeFlashSale` - Tạo/cập nhật flash sale (Admin)
- `DELETE /api/flashsale/{id}` - Xóa flash sale (Admin)
- `GET /api/flashsale/{id}/items` - Lấy items của flash sale

**Authentication**: Một số endpoints yêu cầu Admin role.

Xem chi tiết tại [README_FLASH_SALE.md](./README_FLASH_SALE.md).

---

## 🎫 Coupons

### CouponController
**Route**: `/api/coupon`

**Endpoints**:
- `GET /api/coupon` - Lấy danh sách coupons
- `GET /api/coupon/{id}` - Lấy chi tiết coupon
- `POST /api/coupon` - Tạo coupon mới (Admin)
- `PUT /api/coupon/{id}` - Cập nhật coupon (Admin)
- `DELETE /api/coupon/{id}` - Xóa coupon (Admin)
- `POST /api/coupon/{id}/apply` - Apply coupon (Authenticated)

**Authentication**: Một số endpoints yêu cầu Admin role.

---

## 👥 Users

### UserController
**Route**: `/api/user`

**Endpoints**:
- `GET /api/user/profile` - Lấy profile của user hiện tại (Authenticated)
- `PUT /api/user/profile` - Cập nhật profile (Authenticated)
- `GET /api/user/orders` - Lấy orders của user (Authenticated)
- `GET /api/user/cart` - Lấy cart của user (Authenticated)
- `GET /api/user/wishlist` - Lấy wishlist của user (Authenticated)

**Authentication**: Tất cả endpoints yêu cầu authentication.

---

### AdminController
**Route**: `/api/admin`

**Endpoints**:
- `GET /api/admin/users` - Lấy danh sách users (Admin)
- `GET /api/admin/users/{id}` - Lấy chi tiết user (Admin)
- `PUT /api/admin/users/{id}` - Cập nhật user (Admin)
- `DELETE /api/admin/users/{id}` - Xóa user (Admin)
- `GET /api/admin/stats` - Lấy thống kê tổng quan (Admin)

**Authentication**: Tất cả endpoints yêu cầu Admin role.

---

## 📊 Dashboard

### DashboardController
**Route**: `/api/dashboard`

**Endpoints**:
- `GET /api/dashboard/stats` - Lấy thống kê tổng quan (Admin)
- `GET /api/dashboard/revenue` - Lấy doanh thu theo thời gian (Admin)
- `GET /api/dashboard/products` - Thống kê products (Admin)
- `GET /api/dashboard/orders` - Thống kê orders (Admin)
- `GET /api/dashboard/customers` - Thống kê customers (Admin)

**Authentication**: Tất cả endpoints yêu cầu Admin role.

---

## 🤖 AI & Recommendations

### UserBehaviorController
**Route**: `/api/UserBehavior`

**Endpoints**:
- `POST /api/UserBehavior/track` - Track user behavior (Authenticated)
- `POST /api/UserBehavior/track-anonymous` - Track anonymous behavior
- `GET /api/UserBehavior/history` - Lấy lịch sử behavior (Authenticated)

**Authentication**: Một số endpoints yêu cầu authentication.

---

### RecommendationController
**Route**: `/api/Recommendation`

**Endpoints**:
- `POST /api/Recommendation/get-recommendations` - Lấy recommendations
- `GET /api/Recommendation/content-based/{productId}` - Content-based recommendations
- `GET /api/Recommendation/user-based` - User-based recommendations (Authenticated)

**Authentication**: Một số endpoints yêu cầu authentication.

---

### EmbeddingController
**Route**: `/api/Embedding`

**Endpoints**:
- `POST /api/Embedding/generate-all` - Generate embeddings cho tất cả products (Admin)
- `POST /api/Embedding/generate/{productId}` - Generate embedding cho product (Admin)
- `POST /api/Embedding/test` - Test embedding generation (Admin)

**Authentication**: Tất cả endpoints yêu cầu Admin role.

Xem chi tiết tại [ModelAI/README_BACKEND.md](./ModelAI/README_BACKEND.md).

---

## 📧 Contact

### ContactController
**Route**: `/api/contact`

**Endpoints**:
- `POST /api/contact` - Gửi contact form
- `GET /api/contact` - Lấy danh sách contacts (Admin)
- `GET /api/contact/{id}` - Lấy chi tiết contact (Admin)
- `DELETE /api/contact/{id}` - Xóa contact (Admin)

**Authentication**: Một số endpoints yêu cầu Admin role.

---

## 🔍 Common Patterns

### Pagination

Nhiều endpoints hỗ trợ pagination:

```csharp
[HttpGet]
public async Task<IActionResult> GetItems(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var items = await _context.Items
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    var totalCount = await _context.Items.CountAsync();
    
    return Ok(new
    {
        data = items,
        totalCount,
        page,
        pageSize,
        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    });
}
```

### Filtering

Nhiều endpoints hỗ trợ filtering:

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts(
    [FromQuery] int? categoryId = null,
    [FromQuery] decimal? minPrice = null,
    [FromQuery] decimal? maxPrice = null)
{
    var query = _context.Products.AsQueryable();
    
    if (categoryId.HasValue)
        query = query.Where(p => p.CategoryId == categoryId);
    
    if (minPrice.HasValue)
        query = query.Where(p => p.Price >= minPrice);
    
    if (maxPrice.HasValue)
        query = query.Where(p => p.Price <= maxPrice);
    
    var products = await query.ToListAsync();
    return Ok(products);
}
```

### Include Related Data

Sử dụng `Include()` để load related entities:

```csharp
var order = await _context.Orders
    .Include(o => o.User)
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
    .Include(o => o.Address)
    .FirstOrDefaultAsync(o => o.OrderId == id);
```

## 🛡️ Authorization Attributes

### [Authorize]
Yêu cầu user đã đăng nhập:

```csharp
[Authorize]
[HttpGet("profile")]
public IActionResult GetProfile() { ... }
```

### [Authorize(Roles = "Admin")]
Yêu cầu Admin role:

```csharp
[Authorize(Roles = "Admin")]
[HttpPost]
public IActionResult CreateProduct() { ... }
```

### [AllowAnonymous]
Cho phép truy cập không cần authentication:

```csharp
[AllowAnonymous]
[HttpPost("refresh-token")]
public IActionResult RefreshToken() { ... }
```

## 📝 Response Formats

### Success Response

```json
{
  "data": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20
}
```

### Error Response

```json
{
  "message": "Error message here"
}
```

### Validation Error Response

```json
{
  "errors": {
    "email": ["Email is required"],
    "password": ["Password must be at least 8 characters"]
  }
}
```

## 🔍 Swagger Documentation

Tất cả endpoints được document trong Swagger UI:

- **URL**: `http://localhost:5099/swagger`
- **Authentication**: Click "Authorize" → Nhập `Bearer YOUR_TOKEN`

## 📚 Related Documentation

- [Authentication](./README_AUTHENTICATION.md) - Auth endpoints
- [Database](./README_DATABASE.md) - Models used by controllers
- [Services](./README_SERVICES.md) - Business logic services
- [Payment](./README_PAYMENT.md) - Payment endpoints
- [Shipping](./README_SHIPPING.md) - Shipping endpoints
- [Flash Sale](./README_FLASH_SALE.md) - Flash sale endpoints
- [AI/Recommendations](./ModelAI/README_BACKEND.md) - AI endpoints

---

**Last Updated**: 2024-12-17

