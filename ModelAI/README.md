# ModelAI - Hệ thống Gợi ý Sản phẩm dựa trên Content-Based Filtering

## Tổng quan

Hệ thống AI này sử dụng **Content-Based Filtering** với mô hình BERT để gợi ý các sản phẩm liên quan dựa trên hành vi người dùng (xem, mua, thêm vào giỏ hàng).

## Kiến trúc Pipeline

### 1. Khởi tạo Embedding sản phẩm (Offline)
- Tất cả sản phẩm được mã hóa bằng mô hình BERT (Sentence-BERT hoặc MiniLM)
- Embedding vector (384 hoặc 768 chiều) được lưu vào bảng `ProductEmbeddings`
- Xử lý offline để tối ưu hiệu năng runtime

### 2. Ghi nhận hành vi người dùng (Real-time)
- Mỗi hành vi được ghi lại vào bảng `UserBehaviorLogs`
- Không cần mã hóa lại sản phẩm (đã có sẵn embedding)

### 3. Truy vấn sản phẩm gợi ý (Real-time)
- Tính độ tương đồng cosine giữa embedding sản phẩm hiện tại và các sản phẩm khác
- Trả về Top-K sản phẩm tương đồng nhất

### 4. Cá nhân hóa theo người dùng (Optional)
- Tạo embedding đại diện cho người dùng (trung bình các embedding sản phẩm đã xem)
- Gợi ý dựa trên embedding người dùng

---

## Database Schema

### Bảng: `UserBehaviorLogs`

Lưu trữ tất cả hành vi của người dùng trên hệ thống.

| Attribute | Type | Description | Required |
|-----------|------|-------------|----------|
| `UserBehaviorLogId` | INT (PK) | ID tự động tăng | ✅ |
| `UserId` | INT (FK, nullable) | ID người dùng (null nếu anonymous) | ❌ |
| `ProductId` | INT (FK) | ID sản phẩm | ✅ |
| `BehaviorType` | VARCHAR(50) | Loại hành vi: "view", "add_to_cart", "purchase", "wishlist" | ✅ |
| `ViewDuration` | INT (nullable) | Thời gian xem (giây) - chỉ cho "view" | ❌ |
| `SessionId` | VARCHAR(255) | Session ID để nhóm hành vi | ❌ |
| `IpAddress` | VARCHAR(45) | IP của người dùng | ❌ |
| `UserAgent` | VARCHAR(500) | Browser/device info | ❌ |
| `Metadata` | TEXT (nullable) | JSON string chứa metadata bổ sung | ❌ |
| `CreatedAt` | DATETIME | Thời gian ghi nhận | ✅ |

**Relationships:**
- `UserId` → `Users.Id` (nullable)
- `ProductId` → `Products.ProductId`

**Indexes:**
- Index trên `(UserId, CreatedAt)` để query lịch sử nhanh
- Index trên `(ProductId, BehaviorType)` để phân tích hành vi sản phẩm
- Index trên `SessionId` để nhóm hành vi theo session

---

### Bảng: `ProductEmbeddings`

Lưu trữ embedding vector của từng sản phẩm.

| Attribute | Type | Description | Required |
|-----------|------|-------------|----------|
| `ProductEmbeddingId` | INT (PK) | ID tự động tăng | ✅ |
| `ProductId` | INT (FK, unique) | ID sản phẩm (1-1 với Product) | ✅ |
| `EmbeddingVector` | TEXT/LONGTEXT | JSON array của floats: `[0.123, 0.456, ...]` | ✅ |
| `EmbeddingDimension` | INT | Số chiều vector (384, 768, etc.) | ✅ |
| `ModelName` | VARCHAR(255) | Tên mô hình (ví dụ: "sentence-transformers/all-MiniLM-L6-v2") | ❌ |
| `ModelVersion` | VARCHAR(50) | Version mô hình | ❌ |
| `CreatedAt` | DATETIME | Thời gian tạo embedding | ✅ |
| `UpdatedAt` | DATETIME | Thời gian cập nhật embedding | ✅ |

**Relationships:**
- `ProductId` → `Products.ProductId` (unique, 1-1)

**Indexes:**
- Index trên `ProductId` (unique)

---

## API Endpoints (Backlog)

### 1. Track User Behavior

**Endpoint:** `POST /api/UserBehavior/track`

**Authentication:** Required (JWT Token)

**Request Body:**
```json
{
  "productId": 123,
  "behaviorType": "view",  // "view" | "add_to_cart" | "purchase" | "wishlist"
  "viewDuration": 30,      // Optional: thời gian xem (giây) - chỉ cho "view"
  "sessionId": "abc123",   // Optional: session ID
  "metadata": "{\"referrer\": \"search\", \"category\": \"electronics\"}"  // Optional: JSON string
}
```

**Response:**
```json
{
  "message": "Behavior tracked successfully",
  "behaviorLogId": 456
}
```

---

### 2. Track Anonymous Behavior

**Endpoint:** `POST /api/UserBehavior/track-anonymous`

**Authentication:** Not required

**Request Body:** (giống như `/track`)

**Response:** (giống như `/track`)

---

### 3. Get User Behavior History

**Endpoint:** `GET /api/UserBehavior/history`

**Authentication:** Required (JWT Token)

**Query Parameters:**
- `page` (int, default: 1): Số trang
- `pageSize` (int, default: 20): Số lượng items mỗi trang
- `behaviorType` (string, optional): Lọc theo loại hành vi

**Response:**
```json
{
  "data": [
    {
      "userBehaviorLogId": 1,
      "productId": 123,
      "productName": "iPhone 15",
      "behaviorType": "view",
      "viewDuration": 30,
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### 4. Get Product Recommendations (TODO - Sẽ implement sau)

**Endpoint:** `POST /api/Recommendation/get-recommendations`

**Authentication:** Optional

**Request Body:**
```json
{
  "productId": 123,              // Optional: ID sản phẩm hiện tại
  "userId": 456,                  // Optional: ID người dùng (cho personalization)
  "topK": 10,                     // Số lượng sản phẩm gợi ý (default: 10)
  "recommendationType": "content_based"  // "content_based" | "user_based"
}
```

**Response:**
```json
{
  "recommendations": [
    {
      "productId": 789,
      "name": "Samsung Galaxy S24",
      "price": 999.99,
      "discountPrice": 899.99,
      "heroImage": "https://...",
      "starRating": 4.5,
      "similarityScore": 0.87
    }
  ],
  "recommendationType": "content_based",
  "totalCount": 10
}
```

---

## Workflow Integration

### Frontend Integration

1. **Khi người dùng xem sản phẩm:**
   ```javascript
   // Gọi API khi component ProductDetail mount
   POST /api/UserBehavior/track
   {
     productId: currentProductId,
     behaviorType: "view",
     viewDuration: calculateViewDuration(), // Track thời gian xem
     sessionId: getSessionId()
   }
   ```

2. **Khi người dùng thêm vào giỏ hàng:**
   ```javascript
   POST /api/UserBehavior/track
   {
     productId: productId,
     behaviorType: "add_to_cart"
   }
   ```

3. **Khi người dùng mua hàng:**
   ```javascript
   // Sau khi order thành công
   POST /api/UserBehavior/track
   {
     productId: productId,
     behaviorType: "purchase"
   }
   ```

4. **Lấy sản phẩm gợi ý:**
   ```javascript
   // Trong trang ProductDetail, hiển thị "Sản phẩm liên quan"
   POST /api/Recommendation/get-recommendations
   {
     productId: currentProductId,
     topK: 10,
     recommendationType: "content_based"
   }
   ```

---

## Next Steps (Implementation Plan)

1. ✅ **Tạo Models và DTOs** - Đã hoàn thành
2. ✅ **Tạo Controller cho User Behavior Tracking** - Đã hoàn thành
3. ⏳ **Cập nhật AppDbContext** - Thêm DbSet cho UserBehaviorLogs và ProductEmbeddings
4. ⏳ **Tạo Migration** - Tạo migration cho 2 bảng mới
5. ⏳ **Implement Embedding Service** - Service để tạo embedding từ BERT model
6. ⏳ **Implement Recommendation Service** - Service để tính similarity và trả về recommendations
7. ⏳ **Tạo Recommendation Controller** - Controller cho API gợi ý sản phẩm
8. ⏳ **Background Job** - Job để generate embeddings cho tất cả sản phẩm (offline)
9. ⏳ **Testing** - Test API endpoints và recommendation accuracy

---

## Notes

- **Embedding Model:** Khuyến nghị sử dụng `sentence-transformers/all-MiniLM-L6-v2` (384 dimensions) hoặc `all-mpnet-base-v2` (768 dimensions)
- **Similarity Calculation:** Sử dụng Cosine Similarity để tính độ tương đồng
- **Performance:** Embeddings được tính offline, chỉ cần query và tính similarity ở runtime
- **Scalability:** Có thể sử dụng vector database (Pinecone, Weaviate, Qdrant) nếu số lượng sản phẩm lớn (>100k)

