# Database & Models

## 📋 Tổng quan

EcomerceBE sử dụng **MySQL** làm database và **Entity Framework Core** (EF Core) làm ORM. Database được quản lý thông qua **Migrations** và có cấu trúc phức tạp với nhiều relationships giữa các entities.

## 🗄️ Database Schema

### Core Entities

#### Users & Authentication
- **Users** - User accounts, authentication info
- **OtpCodes** - OTP codes cho password reset
- **Addresses** - User addresses
- **Wallets** - User wallets
- **WalletItems** - Wallet transaction history

#### Products & Catalog
- **Categories** - Product categories (hierarchical)
- **Products** - Product information
- **ProductImages** - Product images
- **ProductSizes** - Product size variants
- **ProductColors** - Product color variants
- **Sizes** - Size definitions
- **CategorySizes** - Category-size relationships
- **Brands** - Brand information
- **Reviews** - Product reviews
- **ReviewImages** - Review images
- **ReviewReplies** - Review replies

#### Orders & Shipping
- **Orders** - Order information
- **OrderItems** - Order line items
- **OrderStatusLogs** - Order status change history
- **Shippings** - Shipping records
- **ShippingMethods** - Shipping method definitions

#### Shopping
- **Carts** - Shopping carts
- **CartItems** - Cart items
- **Wishlists** - User wishlists
- **WishlistItems** - Wishlist items

#### Promotions
- **FlashSales** - Flash sale events
- **FlashSaleItems** - Flash sale products
- **Coupons** - Coupon codes
- **ProductCoupons** - Product-coupon relationships
- **UserCoupons** - User-coupon relationships
- **ShippingMethodCoupons** - Shipping method-coupon relationships

#### AI/Recommendations
- **UserBehaviorLogs** - User behavior tracking
- **ProductEmbeddings** - Product embedding vectors

#### Others
- **Contacts** - Contact form submissions

## 🔗 Relationships Overview

### User Relationships

```
User (1) ──< (N) Address
User (1) ──< (1) Wallet ──< (N) WalletItem
User (1) ──< (N) Cart ──< (N) CartItem
User (1) ──< (N) Wishlist ──< (N) WishlistItem
User (1) ──< (N) Order ──< (N) OrderItem
User (1) ──< (N) Review ──< (N) ReviewImage
User (1) ──< (N) ReviewReply
User (1) ──< (N) UserBehaviorLog
User (N) ──< (N) UserCoupon ──> (N) Coupon
```

### Product Relationships

```
Category (1) ──< (N) Product ──< (N) ProductImage
Category (1) ──< (N) Product ──< (N) ProductSize ──< (N) ProductColor
Category (1) ──< (N) Brand
Category (N) ──< (N) CategorySize ──> (N) Size
Product (1) ──< (N) Review ──< (N) ReviewImage
Product (1) ──< (N) ReviewReply
Product (1) ──< (N) CartItem
Product (1) ──< (N) WishlistItem
Product (1) ──< (N) OrderItem
Product (1) ──< (N) FlashSaleItem
Product (1) ──< (1) ProductEmbedding
Product (1) ──< (N) UserBehaviorLog
Product (N) ──< (N) ProductCoupon ──> (N) Coupon
```

### Order Relationships

```
Order (1) ──< (N) OrderItem ──> (1) Product
Order (1) ──< (1) Shipping
Order (1) ──< (N) OrderStatusLog
Order (N) ──> (1) User
Order (N) ──> (1) Address
Order (N) ──> (1) Coupon
Order (N) ──> (1) ShippingMethod
```

## 📁 Files liên quan

### Database Context

- **`Data/AppDbContext.cs`**
  - `DbSet<T>` declarations cho tất cả entities
  - `OnModelCreating()` - EF Core configuration
  - `SeedAdminUsers()` - Seed admin users on startup
  - `EnsureReviewImagesTable()` - Auto-create ReviewImages table
  - `EnsureReviewRepliesTable()` - Auto-create ReviewReplies table
  - `EnsureReviewsTableHasOrderItemId()` - Add OrderItemId column

### Models

Tất cả models nằm trong `Models/` folder:

- **`Models/Users/`**
  - `User.cs` - User entity
  - `Address.cs` - Address entity
  - `Wallet.cs` - Wallet entity
  - `WalletItem.cs` - WalletItem entity

- **`Models/Products/`**
  - `Product.cs` - Product entity
  - `ProductImage.cs` - ProductImage entity
  - `ProductSize.cs` - ProductSize entity
  - `ProductColor.cs` - ProductColor entity
  - `Category.cs` - Category entity
  - `Size.cs` - Size entity
  - `CategorySize.cs` - CategorySize entity
  - `Brand.cs` - Brand entity

- **`Models/Reviews/`**
  - `Review.cs` - Review entity
  - `ReviewImage.cs` - ReviewImage entity (auto-created)
  - `ReviewReply.cs` - ReviewReply entity (auto-created)

- **`Models/Orders/`**
  - `Order.cs` - Order entity
  - `OrderItem.cs` - OrderItem entity
  - `OrderStatusLog.cs` - OrderStatusLog entity

- **`Models/Shipping/`**
  - `Shipping.cs` - Shipping entity
  - `ShippingMethod.cs` - ShippingMethod entity

- **`Models/Carts/`**
  - `Cart.cs` - Cart entity
  - `CartItem.cs` - CartItem entity

- **`Models/FlashSales/`**
  - `FlashSale.cs` - FlashSale entity
  - `FlashSaleItem.cs` - FlashSaleItem entity

- **`Models/Coupons/`**
  - `Coupon.cs` - Coupon entity

- **`Models/ModelAI/`**
  - `UserBehaviorLog.cs` - UserBehaviorLog entity
  - `ProductEmbedding.cs` - ProductEmbedding entity

- **`Models/Auth/`**
  - `OtpCode.cs` - OtpCode entity

- **`Models/Contacts/`**
  - `Contact.cs` - Contact entity

### Migrations

Tất cả migrations nằm trong `Migrations/` folder:

- `20251111051917_initdtb.cs` - Initial database
- `20251111064034_fixfieldimg.cs` - Fix image fields
- `20251111065148_seedadmin.cs` - Seed admin users
- `20251209021156_AddOrderStatusLog.cs` - Add OrderStatusLog
- `20251209023509_AddShippingModel.cs` - Add Shipping models
- `20251217050040_AddModelAITables.cs` - Add AI tables
- ... và nhiều migrations khác

## ⚙️ Configuration

### Connection String

**`appsettings.json`**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=EcomerceBE;user=root;password=YOUR_PASSWORD"
  }
}
```

### Program.cs Configuration

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
```

## 🔧 Entity Framework Configuration

### Decimal Precision

Tất cả giá tiền sử dụng `decimal(18,2)`:

```csharp
modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
modelBuilder.Entity<Coupon>().Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
modelBuilder.Entity<FlashSaleItem>().Property(f => f.DiscountPrice).HasColumnType("decimal(18,2)");
modelBuilder.Entity<Wallet>().Property(w => w.Balance).HasColumnType("decimal(18,2)");
modelBuilder.Entity<WalletItem>().Property(wi => wi.Amount).HasColumnType("decimal(18,2)");
```

### Unique Constraints

```csharp
// Email uniqueness (optional, currently not enforced)
modelBuilder.Entity<User>()
    .HasIndex(u => u.Email)
    .IsUnique(false);

// Coupon code uniqueness
modelBuilder.Entity<Coupon>()
    .HasIndex(c => c.Code)
    .IsUnique(true);

// ProductSize uniqueness
modelBuilder.Entity<ProductSize>()
    .HasIndex(ps => new { ps.ProductId, ps.SizeId, ps.CustomValue })
    .IsUnique();

// ProductColor uniqueness
modelBuilder.Entity<ProductColor>()
    .HasIndex(pc => new { pc.ProductSizeId, pc.ColorCode })
    .IsUnique();

// ProductEmbedding uniqueness
modelBuilder.Entity<ProductEmbedding>()
    .HasIndex(pe => pe.ProductId)
    .IsUnique();
```

### Delete Behaviors

- **Cascade**: Xóa parent → Xóa children
  - User → Carts, Wishlists, Orders, Reviews
  - Product → ProductImages, ProductSizes, Reviews
  - Order → OrderItems, OrderStatusLogs, Shipping

- **SetNull**: Xóa parent → Set foreign key = null
  - Category → Products (CategoryId = null)
  - Address → Orders (AddressId = null)
  - Coupon → Orders (CouponId = null)

- **Restrict**: Không cho xóa parent nếu có children
  - Product → CartItems, OrderItems
  - Size → ProductSizes

### Indexes

```csharp
// UserBehaviorLog indexes
modelBuilder.Entity<UserBehaviorLog>()
    .HasIndex(ubl => new { ubl.UserId, ubl.CreatedAt })
    .HasDatabaseName("IX_UserBehaviorLogs_UserId_CreatedAt");

modelBuilder.Entity<UserBehaviorLog>()
    .HasIndex(ubl => new { ubl.ProductId, ubl.BehaviorType })
    .HasDatabaseName("IX_UserBehaviorLogs_ProductId_BehaviorType");

modelBuilder.Entity<UserBehaviorLog>()
    .HasIndex(ubl => ubl.SessionId)
    .HasDatabaseName("IX_UserBehaviorLogs_SessionId");
```

## 🚀 Migrations

### Tạo Migration

```bash
dotnet ef migrations add MigrationName
```

### Apply Migrations

```bash
dotnet ef database update
```

### Xóa Migration cuối cùng

```bash
dotnet ef migrations remove
```

### Rollback Migration

```bash
dotnet ef database update PreviousMigrationName
```

## 🌱 Seed Data

### Admin Users

Admin users được tự động seed khi ứng dụng khởi động:

```csharp
public void SeedAdminUsers()
{
    if (!Users.Any(u => u.Email == "Hungv5996@gmail.com"))
    {
        var adminPass = BCrypt.Net.BCrypt.HashPassword("admin@1234");
        Users.Add(new User
        {
            Email = "Hungv5996@gmail.com",
            PasswordHash = adminPass,
            Role = "Admin",
            Name = "Hungadmin"
        });
    }
    // ... more admin users
}
```

**Default Admin Accounts**:
- Email: `Hungv5996@gmail.com` / Password: `admin@1234`
- Email: `thaithanhphat323@gmail.com` / Password: `admin@1234`

### Auto-create Tables

Một số tables được tự động tạo nếu chưa tồn tại:

- **ReviewImages** - Tự động tạo khi app khởi động
- **ReviewReplies** - Tự động tạo khi app khởi động
- **OrderItemId column** - Tự động thêm vào Reviews table nếu chưa có

## 📊 Database Schema Details

### Users Table

| Column | Type | Description |
|--------|------|-------------|
| Id | INT (PK) | User ID |
| Email | VARCHAR | Email address |
| PasswordHash | VARCHAR | Hashed password |
| Name | VARCHAR | User name |
| Role | VARCHAR | "Admin" or "User" |
| RefreshToken | VARCHAR | Refresh token |
| RefreshTokenExpiryTime | DATETIME | Refresh token expiry |
| EmailVerificationToken | VARCHAR | Email verification token |
| EmailVerificationExpiry | DATETIME | Token expiry |
| status | VARCHAR | "Active" or "Pending" |
| Avatar | VARCHAR | Avatar URL |
| CreatedAt | DATETIME | Created timestamp |

### Products Table

| Column | Type | Description |
|--------|------|-------------|
| ProductId | INT (PK) | Product ID |
| Name | VARCHAR | Product name |
| Description | TEXT | Product description |
| Price | DECIMAL(18,2) | Original price |
| DiscountPrice | DECIMAL(18,2) | Discounted price |
| CategoryId | INT (FK) | Category ID |
| HeroImage | VARCHAR | Main product image |
| ProductType | VARCHAR | Product type |
| CreatedAt | DATETIME | Created timestamp |

### Orders Table

| Column | Type | Description |
|--------|------|-------------|
| OrderId | INT (PK) | Order ID |
| OrderNumber | VARCHAR | Unique order number |
| UserId | INT (FK) | User ID |
| AddressId | INT (FK, nullable) | Address ID |
| CouponId | INT (FK, nullable) | Coupon ID |
| ShippingMethodId | INT (FK, nullable) | Shipping method ID |
| TotalAmount | DECIMAL(18,2) | Total amount |
| Status | VARCHAR | Order status |
| CreatedAt | DATETIME | Created timestamp |

## 🔍 Query Examples

### Include Related Data

```csharp
// Include product images
var product = await _context.Products
    .Include(p => p.Images)
    .FirstOrDefaultAsync(p => p.ProductId == id);

// Include order items with products
var order = await _context.Orders
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
    .Include(o => o.User)
    .Include(o => o.Address)
    .FirstOrDefaultAsync(o => o.OrderId == id);
```

### Filtering & Pagination

```csharp
var products = await _context.Products
    .Where(p => p.CategoryId == categoryId)
    .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
    .OrderByDescending(p => p.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## 🐛 Troubleshooting

### Lỗi: "Table doesn't exist"

- Chạy migrations: `dotnet ef database update`
- Kiểm tra connection string đúng database name

### Lỗi: "Column doesn't exist"

- Tạo migration mới: `dotnet ef migrations add AddColumnName`
- Apply migration: `dotnet ef database update`

### Lỗi: "Foreign key constraint fails"

- Kiểm tra delete behavior trong `OnModelCreating`
- Đảm bảo foreign key values tồn tại trong parent table

### Lỗi: "Duplicate entry"

- Kiểm tra unique constraints
- Đảm bảo không insert duplicate values

## 📚 Related Documentation

- [Authentication](./README_AUTHENTICATION.md) - User model details
- [API Controllers](./README_API_CONTROLLERS.md) - How controllers use models
- [Services](./README_SERVICES.md) - Business logic with models

---

**Last Updated**: 2024-12-17

