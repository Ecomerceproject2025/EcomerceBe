## 📦 Hướng dẫn import sản phẩm bằng Excel

Tài liệu này mô tả **đúng format** file Excel dùng cho API:

- **Endpoint**: `POST /api/product/import-products`  
- **Body**: `multipart/form-data` với field `file` (kiểu `.xlsx`)

---

## 1. Cấu trúc file Excel

- Định dạng: **`.xlsx`**
- Dùng **sheet đầu tiên**
- **Dòng 1**: header
- **Dòng 2 trở đi**: dữ liệu

### 1.1. Danh sách cột (theo thứ tự)

| STT | Cột                  | Bắt buộc | Kiểu           | Mô tả                                                                                          |
|-----|----------------------|----------|----------------|------------------------------------------------------------------------------------------------|
| 1   | `Name`               | ✅       | string         | Tên sản phẩm                                                                                   |
| 2   | `Description`        | ❌       | string         | Mô tả sản phẩm                                                                                 |
| 3   | `ImportPrice`        | ❌       | decimal ≥ 0    | Giá nhập                                                                                       |
| 4   | `Price`              | ✅       | decimal > 0    | Giá bán                                                                                         |
| 5   | `SalePrice`          | ❌       | decimal        | Giá khuyến mãi (nếu > 0 thì **không được lớn hơn** `Price`)                                   |
| 6   | `ReturnDeliveryDay`  | ❌       | int            | Số ngày cho phép đổi trả                                                                      |
| 7   | `CategoryId`         | ✅       | int            | Id category tồn tại trong DB                                                                   |
| 8   | `Images`             | ❌       | string (CSV)   | Danh sách URL ảnh, phân tách bằng `,`                                                          |
| 9   | `Coupons`            | ❌       | string (CSV)   | Danh sách coupon, format `CODE:Description:Discount:IsActive`, nhiều coupon ngăn bằng `\|`    |
| 10  | `Variants`           | ❌       | string (CSV)   | Danh sách biến thể (size/màu/số lượng), format chi tiết bên dưới                              |
| 11  | `productType`        | ❌       | string         | `Normal` hoặc `FlashSale` (để trống mặc định `Normal`)                                         |
| 12  | `ProductSaleQuantity`| ❌       | int ≥ 0        | Tổng số lượng dùng cho Flash Sale cấp product (fallback nếu biến thể không có saleQty)       |

---

## 2. Định dạng cột Variants (cột 10)

- Mỗi biến thể có format:

```text
type:value:#color:qty[:saleQty]
```

- Nhiều biến thể được nối bằng ký tự `|`.

**Ý nghĩa:**

- `type`:  
  - `select` – dùng size có sẵn (S, M, L, XL, …)  
  - `custom` – size custom (EU42, 15-inch, …)
- `value`: giá trị size (VD: `M`, `L`, `EU42`)
- `#color`: mã màu hex, VD: `#FF0000`
- `qty`: tồn kho của biến thể
- `saleQty` (tùy chọn): tồn kho dành riêng cho **flash sale** của biến thể

**Ví dụ:**

```text
select:M:#FF0000:10|select:L:#00FF00:5
```

Biến thể có flash sale:

```text
select:M:#FF0000:10:3|custom:EU42:#000000:8:2
```

---

## 3. Quy tắc cho Flash Sale

Flash sale hoạt động dựa trên các trường:
- `productType`
- `ProductSaleQuantity`
- `VariantVm.saleQuantity` (từ `saleQty` trong cột Variants)

### 3.1. Kích hoạt Flash Sale

- Để sản phẩm là flash sale:  
  - Cột **`productType` = `FlashSale`** (không phân biệt hoa thường, ví dụ `flashsale`, `FlashSale`, …)

### 3.2. Số lượng Flash Sale

- **Cấp biến thể**: nếu trong cột Variants có `saleQty`, hệ thống sẽ:
  - Lưu `saleQty` vào `ProductColor.SaleQuantity`
  - Tính tổng các `saleQty` để làm `saleQuantity` cho `FlashSaleItem`

- **Cấp sản phẩm**: nếu **tổng `saleQty` biến thể = 0** và `ProductSaleQuantity` (cột 12) > 0:
  - Hệ thống dùng `ProductSaleQuantity` làm `FlashSaleItem.saleQuantity`

- Nếu cả hai đều 0 → sản phẩm flash sale nhưng **không có tồn sale**, khách có thể không mua được.

---

## 4. Ví dụ dòng dữ liệu

### 4.1. Sản phẩm thường (không flash sale)

```text
Tshirt Local Brand | Áo thun local brand basic | 100000 | 199000 | 0 | 7 | 3 | https://img.com/a.jpg,https://img.com/b.jpg | SALE10:Giảm 10k:10000:1 | select:M:#FF0000:10|select:L:#00FF00:5 | Normal | 
```

Giải thích nhanh:
- Name = `Tshirt Local Brand`
- Price = 199000, không có SalePrice
- CategoryId = 3
- 2 biến thể (M đỏ 10 cái, L xanh 5 cái)
- productType = Normal

### 4.2. Sản phẩm Flash Sale với saleQty theo biến thể

```text
Tshirt Flash | Áo thun flash sale | 80000 | 150000 | 120000 | 7 | 3 | https://img.com/a.jpg |  | select:M:#FF0000:10:3|select:L:#000000:8:2 | FlashSale | 
```

- `productType = FlashSale`
- Biến thể:
  - M đỏ: qty = 10, saleQty = 3
  - L đen: qty = 8, saleQty = 2
- Tổng saleQty = 5 (tự động dùng cho `FlashSaleItem.saleQuantity`)
- Cột `ProductSaleQuantity` trống → không dùng.

### 4.3. Sản phẩm Flash Sale dùng ProductSaleQuantity

```text
Headphone X | Tai nghe flash sale | 500000 | 700000 | 650000 | 14 | 5 | https://img.com/hp.jpg |  | select:Default:#000000:20 | FlashSale | 10
```

- `productType = FlashSale`
- 1 biến thể mặc định, không có `saleQty` riêng
- `ProductSaleQuantity = 10` → toàn bộ 10 chiếc dùng cho flash sale.

---

## 5. Validation & Lỗi thường gặp

Trong quá trình import, backend sẽ kiểm tra:

- **Name** trống → bỏ qua dòng, log: `Row X: Name is required.`
- **CategoryId** không phải số → `Row X: categoryId is required and must be an integer.`
- **Price ≤ 0** → `Row X: Price must be greater than 0.`
- **ImportPrice < 0** → `Row X: Import price cannot be negative.`
- **SalePrice > Price** → `Row X: Sale price cannot be higher than the regular price.`
- Biến thể format sai (không đủ 4–5 phần) → biến thể đó bị bỏ qua (dòng vẫn có thể import nếu còn dữ liệu hợp lệ).

Kết quả trả về:

```json
{
  "message": "Import completed",
  "summary": {
    "successCount": 10,
    "errorCount": 2,
    "totalRows": 12
  },
  "errors": [
    "Row 3: Price must be greater than 0.",
    "Row 5: categoryId is required and must be an integer."
  ]
}
```

---

## 6. Gợi ý workflow

1. Tải file mẫu (có đầy đủ 12 cột header).
2. Điền dữ liệu theo đúng format trên.
3. Gửi lên endpoint `POST /api/product/import-products` với field `file`.
4. Đọc `errors` trong response để biết dòng nào lỗi và nội dung lỗi.


