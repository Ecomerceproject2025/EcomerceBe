# ModelAI - Hướng dẫn Backend Implementation

## 📋 Mục lục

1. [Tổng quan](#tổng-quan)
2. [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
3. [Setup và Cấu hình](#setup-và-cấu-hình)
4. [Database Schema](#database-schema)
5. [API Endpoints](#api-endpoints)
6. [Implementation Guide](#implementation-guide)
7. [Testing](#testing)
8. [Troubleshooting](#troubleshooting)

---

## Tổng quan

Hệ thống ModelAI sử dụng **Content-Based Filtering** với mô hình BERT để gợi ý sản phẩm liên quan dựa trên:

- Hành vi người dùng (xem, mua, thêm vào giỏ hàng)
- Độ tương đồng giữa các sản phẩm (dựa trên embedding vectors)

### Công nghệ sử dụng

- **Backend**: ASP.NET Core 9, C#
- **ORM**: Entity Framework Core
- **Database**: MySQL
- **AI Model**: Sentence-BERT (sentence-transformers/all-MiniLM-L6-v2)
- **Similarity**: Cosine Similarity

---

## Kiến trúc hệ thống

### Pipeline hoạt động

```
┌─────────────────────────────────────────────────────────────┐
│ 1. OFFLINE: Generate Product Embeddings                     │
│    - Tất cả sản phẩm được mã hóa bằng BERT                  │
│    - Embedding vectors lưu vào ProductEmbeddings table      │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. REAL-TIME: Track User Behavior                           │
│    - User xem/mua/thêm vào giỏ → ghi vào UserBehaviorLogs  │
│    - Không cần mã hóa lại (đã có sẵn embedding)             │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. REAL-TIME: Get Recommendations                           │
│    - Tính cosine similarity giữa embeddings                │
│    - Trả về Top-K sản phẩm tương đồng nhất                 │
└─────────────────────────────────────────────────────────────┘
```

### Flow chi tiết: Frontend → Backend → AI Model

```
┌─────────────────────────────────────────────────────────────────────┐
│ FRONTEND (React/Next.js)                                             │
│                                                                      │
│ 1. User xem sản phẩm                                                 │
│    → POST /api/UserBehavior/track                                   │
│    { productId: 123, behaviorType: "view" }                         │
│                                                                      │
│ 2. User muốn xem sản phẩm gợi ý                                     │
│    → POST /api/Recommendation/get-recommendations                   │
│    { productId: 123, topK: 10, recommendationType: "content_based" }│
└─────────────────────────────────────────────────────────────────────┘
                          ↓ HTTP Request
┌─────────────────────────────────────────────────────────────────────┐
│ BACKEND (ASP.NET Core)                                               │
│                                                                      │
│ UserBehaviorController                                                │
│   → Lưu behavior vào UserBehaviorLogs table                         │
│                                                                      │
│ RecommendationController                                             │
│   → Gọi RecommendationService                                       │
│     → Query ProductEmbeddings từ database                           │
│     → Tính Cosine Similarity                                        │
│     → Trả về Top-K sản phẩm                                         │
└─────────────────────────────────────────────────────────────────────┘
                          ↓ (Khi generate embeddings)
┌─────────────────────────────────────────────────────────────────────┐
│ AI MODEL (Python FastAPI / ML.NET / External API)                    │
│                                                                      │
│ EmbeddingService                                                      │
│   → POST {aiModelApiUrl}/api/embedding/generate                     │
│   { text: "iPhone 15 Pro Max - Flagship smartphone..." }            │
│                                                                      │
│ AI Model (BERT/Sentence-BERT)                                        │
│   → Generate embedding vector [0.123, 0.456, ...] (384 dimensions)  │
│   → Return JSON response                                             │
└─────────────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────────────┐
│ DATABASE (MySQL)                                                     │
│                                                                      │
│ ProductEmbeddings table                                              │
│   - Lưu embedding vectors dưới dạng JSON string                      │
│   - Index trên ProductId để query nhanh                             │
│                                                                      │
│ UserBehaviorLogs table                                               │
│   - Lưu hành vi người dùng                                           │
│   - Index trên (UserId, CreatedAt) và (ProductId, BehaviorType)     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Setup và Cấu hình

### Luồng FE ↔ BE ↔ AI (tóm tắt kết nối)

- **FE → BE**

  - Base URL FE: `NEXT_PUBLIC_API_URL = http://localhost:5099/api`
  - Track hành vi:
    - Auth: `POST /UserBehavior/track`
    - Anonymous: `POST /UserBehavior/track-anonymous`
  - Gợi ý:
    - Content-based: `GET /Recommendation/content-based/{productId}?topK=10`
    - User-based: `GET /Recommendation/user-based?topK=10`

- **BE → AI**

  - `appsettings.json`
    ```json
    "AIModel": {
      "ApiUrl": "http://localhost:8000",
      "ApiKey": ""
    }
    ```
  - `EmbeddingService` / `RecommendationService` gọi AI Model API (FastAPI) qua `ApiUrl`.

- **Chạy dịch vụ**

  - AI server: `.\.venv\Scripts\python.exe .\ModelAI\main.py` (docs: `http://localhost:8000/docs`)
  - BE server: `dotnet run` (swagger: `http://localhost:5099/swagger`)

- **Vị trí mã nguồn**

  - FE:
    - Hooks tracking/recommend: `src/hooks/useUserBehavior.ts`, `src/hooks/useRecommendations.ts`
    - UI gợi ý: Product Detail block (content-based), Home `RecommendedForYou` (user-based) `src/components/homePage/recommend/`
  - BE:
    - Controllers: `Controllers/ModelAI/UserBehaviorController.cs`, `Controllers/ModelAI/RecommendationController.cs`
    - Services: `Service/ModelAI/EmbeddingService.cs`, `Service/ModelAI/RecommendationService.cs`
    - Background: `Background/EmbeddingGenerationService.cs`
  - AI (Python): `ModelAI/main.py`

- **Luồng runtime nhanh**
  1. User mở Product Detail → FE auto track view, gọi content-based recommend.
  2. User add to cart/purchase → FE track → BE lưu log.
  3. User mở Home (đã login) → FE gọi user-based recommend.
  4. BE nếu thiếu embedding sẽ gọi AI sinh embedding; nếu có sẵn thì trả gợi ý ngay.

### 1. Kiểm tra Models và DbContext

Đảm bảo các Models đã được tạo:

- ✅ `Models/ModelAI/UserBehaviorLog.cs`
- ✅ `Models/ModelAI/ProductEmbedding.cs`

Đảm bảo `AppDbContext.cs` đã có:

```csharp
public DbSet<UserBehaviorLog> UserBehaviorLogs { get; set; }
public DbSet<ProductEmbedding> ProductEmbeddings { get; set; }
```

### 2. Tạo Migration

**Lưu ý:** Nếu `AppDbContext` đã có `DbSet<UserBehaviorLog>` và `DbSet<ProductEmbedding>`, bạn cần tạo migration:

```bash
# Tạo migration mới
dotnet ef migrations add AddModelAITables

# Áp dụng migration vào database
dotnet ef database update
```

**Kiểm tra migration:**

- Mở file migration trong `Migrations/` folder
- Đảm bảo có tạo 2 bảng: `UserBehaviorLogs` và `ProductEmbeddings`
- Đảm bảo có các indexes đã được định nghĩa trong `AppDbContext.OnModelCreating`

### 3. Kiểm tra Indexes

Sau khi migration, kiểm tra các indexes đã được tạo:

- `IX_UserBehaviorLogs_UserId_CreatedAt`
- `IX_UserBehaviorLogs_ProductId_BehaviorType`
- `IX_UserBehaviorLogs_SessionId`
- `IX_ProductEmbeddings_ProductId` (unique)

### 4. Cấu hình trong Program.cs

Đảm bảo các services đã được đăng ký:

```csharp
// ✅ ModelAI Services
builder.Services.AddHttpClient(); // For EmbeddingService to call AI Model API
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
```

### 5. Cấu hình AI Model API (Optional)

Trong `appsettings.json`, thêm cấu hình cho AI Model API:

```json
{
  "AIModel": {
    "ApiUrl": "http://localhost:8000" // URL của AI Model API (Python FastAPI)
  }
}
```

**Lưu ý:**

- Nếu không có AI Model API, hệ thống sẽ sử dụng mock embedding (để test)
- Trong production, nên có AI Model API riêng hoặc sử dụng ML.NET với ONNX model

---

## Database Schema

### Bảng: `UserBehaviorLogs`

| Column              | Type               | Description                                   |
| ------------------- | ------------------ | --------------------------------------------- |
| `UserBehaviorLogId` | INT (PK)           | ID tự động tăng                               |
| `UserId`            | INT (FK, nullable) | ID người dùng (null nếu anonymous)            |
| `ProductId`         | INT (FK)           | ID sản phẩm                                   |
| `BehaviorType`      | VARCHAR(50)        | "view", "add_to_cart", "purchase", "wishlist" |
| `ViewDuration`      | INT (nullable)     | Thời gian xem (giây) - chỉ cho "view"         |
| `SessionId`         | VARCHAR(255)       | Session ID để nhóm hành vi                    |
| `IpAddress`         | VARCHAR(45)        | IP của người dùng                             |
| `UserAgent`         | VARCHAR(500)       | Browser/device info                           |
| `Metadata`          | TEXT (nullable)    | JSON string chứa metadata bổ sung             |
| `CreatedAt`         | DATETIME           | Thời gian ghi nhận                            |

**Relationships:**

- `UserId` → `Users.Id` (nullable)
- `ProductId` → `Products.ProductId`

### Bảng: `ProductEmbeddings`

| Column               | Type             | Description                                                   |
| -------------------- | ---------------- | ------------------------------------------------------------- |
| `ProductEmbeddingId` | INT (PK)         | ID tự động tăng                                               |
| `ProductId`          | INT (FK, unique) | ID sản phẩm (1-1 với Product)                                 |
| `EmbeddingVector`    | LONGTEXT         | JSON array của floats: `[0.123, 0.456, ...]`                  |
| `EmbeddingDimension` | INT              | Số chiều vector (384, 768, etc.)                              |
| `ModelName`          | VARCHAR(255)     | Tên mô hình (ví dụ: "sentence-transformers/all-MiniLM-L6-v2") |
| `ModelVersion`       | VARCHAR(50)      | Version mô hình                                               |
| `CreatedAt`          | DATETIME         | Thời gian tạo embedding                                       |
| `UpdatedAt`          | DATETIME         | Thời gian cập nhật embedding                                  |

**Relationships:**

- `ProductId` → `Products.ProductId` (unique, 1-1)

---

## API Endpoints

### 1. Track User Behavior (Authenticated)

**Endpoint:** `POST /api/UserBehavior/track`

**Authentication:** Required (JWT Token)

**Request Body:**

```json
{
  "productId": 123,
  "behaviorType": "view", // "view" | "add_to_cart" | "purchase" | "wishlist"
  "viewDuration": 30, // Optional: thời gian xem (giây) - chỉ cho "view"
  "sessionId": "abc123", // Optional: session ID
  "metadata": "{\"referrer\": \"search\", \"category\": \"electronics\"}" // Optional: JSON string
}
```

**Response:**

```json
{
  "message": "Behavior tracked successfully",
  "behaviorLogId": 456
}
```

**Status Codes:**

- `200 OK`: Thành công
- `400 Bad Request`: Dữ liệu không hợp lệ
- `401 Unauthorized`: Chưa đăng nhập
- `404 Not Found`: Sản phẩm không tồn tại
- `500 Internal Server Error`: Lỗi server

---

### 2. Track Anonymous Behavior

**Endpoint:** `POST /api/UserBehavior/track-anonymous`

**Authentication:** Not required

**Request Body:** (giống như `/track`)

**Response:** (giống như `/track`)

**Status Codes:**

- `200 OK`: Thành công
- `400 Bad Request`: Dữ liệu không hợp lệ
- `404 Not Found`: Sản phẩm không tồn tại
- `500 Internal Server Error`: Lỗi server

---

### 3. Get User Behavior History

**Endpoint:** `GET /api/UserBehavior/history`

**Authentication:** Required (JWT Token)

**Query Parameters:**

- `page` (int, default: 1): Số trang
- `pageSize` (int, default: 20): Số lượng items mỗi trang
- `behaviorType` (string, optional): Lọc theo loại hành vi ("view", "add_to_cart", "purchase", "wishlist")

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

**Status Codes:**

- `200 OK`: Thành công
- `401 Unauthorized`: Chưa đăng nhập
- `500 Internal Server Error`: Lỗi server

---

### 4. Get Product Recommendations

**Endpoint:** `POST /api/Recommendation/get-recommendations`

**Authentication:** Optional (cần cho user-based recommendations)

**Request Body:**

```json
{
  "productId": 123, // Optional: ID sản phẩm hiện tại (cho content-based)
  "userId": 456, // Optional: ID người dùng (cho user-based)
  "topK": 10, // Số lượng sản phẩm gợi ý (default: 10)
  "recommendationType": "content_based" // "content_based" | "user_based"
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

**Status Codes:**

- `200 OK`: Thành công
- `400 Bad Request`: Dữ liệu không hợp lệ
- `404 Not Found`: Sản phẩm không tồn tại hoặc chưa có embedding
- `500 Internal Server Error`: Lỗi server

---

### 5. Get Content-Based Recommendations (Simplified)

**Endpoint:** `GET /api/Recommendation/content-based/{productId}?topK=10`

**Authentication:** Not required

**Query Parameters:**

- `topK` (int, default: 10): Số lượng sản phẩm gợi ý

**Response:** (giống như `/get-recommendations`)

---

### 6. Get User-Based Recommendations (Simplified)

**Endpoint:** `GET /api/Recommendation/user-based?topK=10`

**Authentication:** Required (JWT Token)

**Query Parameters:**

- `topK` (int, default: 10): Số lượng sản phẩm gợi ý

**Response:** (giống như `/get-recommendations`)

---

### 7. Generate Embeddings for All Products (Admin Only)

**Endpoint:** `POST /api/Embedding/generate-all`

**Authentication:** Required (JWT Token, Admin role)

**Description:** Generate embeddings cho tất cả sản phẩm chưa có embedding

**Response:**

```json
{
  "message": "Successfully generated embeddings for 50 products",
  "count": 50
}
```

**Status Codes:**

- `200 OK`: Thành công
- `401 Unauthorized`: Chưa đăng nhập hoặc không phải Admin
- `500 Internal Server Error`: Lỗi server

---

### 8. Generate Embedding for Single Product (Admin Only)

**Endpoint:** `POST /api/Embedding/generate/{productId}`

**Authentication:** Required (JWT Token, Admin role)

**Description:** Generate hoặc update embedding cho một sản phẩm cụ thể

**Response:**

```json
{
  "message": "Successfully generated embedding for product 123",
  "productId": 123
}
```

**Status Codes:**

- `200 OK`: Thành công
- `401 Unauthorized`: Chưa đăng nhập hoặc không phải Admin
- `404 Not Found`: Sản phẩm không tồn tại
- `500 Internal Server Error`: Lỗi server

---

### 9. Test Embedding Generation (Admin Only)

**Endpoint:** `POST /api/Embedding/test`

**Authentication:** Required (JWT Token, Admin role)

**Request Body:**

```json
"iPhone 15 Pro Max - Flagship smartphone with A17 chip"
```

**Response:**

```json
{
  "message": "Embedding generated successfully",
  "dimension": 384,
  "sample": [0.123, 0.456, 0.789, 0.012, 0.345],
  "note": "This is a test endpoint. Full embedding vector is not returned."
}
```

**Status Codes:**

- `200 OK`: Thành công
- `400 Bad Request`: Text không được để trống
- `401 Unauthorized`: Chưa đăng nhập hoặc không phải Admin
- `500 Internal Server Error`: Lỗi server

---

## Implementation Guide

### ✅ Đã hoàn thành

1. **Models và DTOs** - ✅ Đã có
2. **UserBehaviorController** - ✅ Đã implement
3. **EmbeddingService** - ✅ Đã implement (có thể gọi AI Model API hoặc dùng mock)
4. **RecommendationService** - ✅ Đã implement (với cosine similarity)
5. **RecommendationController** - ✅ Đã implement
6. **AppDbContext Configuration** - ✅ Đã có DbSet và indexes

### Bước 1: Tạo Embedding Service (✅ Đã hoàn thành)

Tạo service để generate embeddings từ BERT model:

**File:** `Service/ModelAI/IEmbeddingService.cs` - ✅ Đã tạo
**File:** `Service/ModelAI/EmbeddingService.cs` - ✅ Đã implement

**Tính năng:**

- Gọi AI Model API qua HTTP (nếu có `AIModel:ApiUrl` trong appsettings.json)
- Fallback về mock embedding nếu không có API (để test)
- Generate embeddings cho tất cả sản phẩm chưa có embedding
- Update embedding cho sản phẩm cụ thể

**Lưu ý:**

- Có thể sử dụng Python FastAPI service để host BERT model
- Hoặc sử dụng ML.NET với ONNX model
- Hoặc gọi external API (Hugging Face Inference API)

---

### Bước 2: Tạo Recommendation Service (✅ Đã hoàn thành)

Tạo service để tính similarity và trả về recommendations:

**File:** `Service/ModelAI/IRecommendationService.cs` - ✅ Đã tạo
**File:** `Service/ModelAI/RecommendationService.cs` - ✅ Đã implement

**Tính năng:**

- **Content-Based Recommendations**: Gợi ý sản phẩm tương đồng với sản phẩm hiện tại
- **User-Based Recommendations**: Gợi ý sản phẩm dựa trên lịch sử người dùng
- Tính cosine similarity giữa embedding vectors
- Parse embedding từ JSON string
- Tính star rating từ Reviews

---

### Bước 3: Tạo Recommendation Controller (✅ Đã hoàn thành)

**File:** `Controllers/ModelAI/RecommendationController.cs` - ✅ Đã implement

**Endpoints:**

- `POST /api/Recommendation/get-recommendations` - Lấy recommendations (content-based hoặc user-based)
- `GET /api/Recommendation/content-based/{productId}` - Simplified endpoint cho content-based
- `GET /api/Recommendation/user-based` - Simplified endpoint cho user-based (yêu cầu authentication)

---

### Bước 4: Đăng ký Services trong Program.cs (✅ Đã hoàn thành)

```csharp
// ✅ ModelAI Services
builder.Services.AddHttpClient(); // For EmbeddingService to call AI Model API
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
```

---

### Bước 5: Background Job để Generate Embeddings (✅ Đã hoàn thành)

**File:** `Background/EmbeddingGenerationService.cs` - ✅ Đã tạo

**Tính năng:**

- Tự động chạy mỗi 6 giờ để generate embeddings cho sản phẩm mới
- Chạy ngay khi ứng dụng khởi động (lần đầu)
- Đã được đăng ký trong `Program.cs`

---

## Testing

### 1. Test User Behavior Tracking

```bash
# Test authenticated tracking
curl -X POST http://localhost:5000/api/UserBehavior/track \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "productId": 1,
    "behaviorType": "view",
    "viewDuration": 30
  }'

# Test anonymous tracking
curl -X POST http://localhost:5000/api/UserBehavior/track-anonymous \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 1,
    "behaviorType": "add_to_cart"
  }'
```

### 2. Test Get Behavior History

```bash
curl -X GET "http://localhost:5000/api/UserBehavior/history?page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3. Test Recommendations

```bash
# Test content-based recommendations
curl -X POST http://localhost:5000/api/Recommendation/get-recommendations \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 1,
    "topK": 10,
    "recommendationType": "content_based"
  }'

# Test simplified content-based endpoint
curl -X GET "http://localhost:5000/api/Recommendation/content-based/1?topK=10"

# Test user-based recommendations (cần JWT token)
curl -X GET "http://localhost:5000/api/Recommendation/user-based?topK=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4. Test Generate Embeddings (nếu có AI Model API)

```bash
# Gọi API để generate embeddings cho tất cả sản phẩm
# (Cần tạo endpoint riêng hoặc gọi từ service)
```

---

## Troubleshooting

### Lỗi: "Product embedding not found"

**Nguyên nhân:** Sản phẩm chưa có embedding vector.

**Giải pháp:**

1. Chạy background job để generate embeddings
2. Hoặc gọi API để generate embedding cho sản phẩm cụ thể

### Lỗi: "Invalid embedding vector"

**Nguyên nhân:** Embedding vector không đúng format JSON array.

**Giải pháp:**

- Kiểm tra format của `EmbeddingVector` trong database
- Đảm bảo là JSON array: `[0.123, 0.456, ...]`

### Lỗi: Performance chậm khi query recommendations

**Nguyên nhân:** Số lượng sản phẩm quá lớn, tính similarity cho tất cả sản phẩm.

**Giải pháp:**

1. Sử dụng vector database (Pinecone, Weaviate, Qdrant) nếu >100k sản phẩm
2. Hoặc cache kết quả recommendations
3. Hoặc giới hạn số lượng sản phẩm tính similarity (ví dụ: chỉ tính cho sản phẩm cùng category)

### Lỗi: Migration không chạy

**Giải pháp:**

```bash
# Xóa migration cũ (nếu có)
dotnet ef migrations remove

# Tạo lại migration
dotnet ef migrations add AddModelAITables

# Apply migration
dotnet ef database update
```

---

## Next Steps

1. ✅ **Models và DTOs** - Đã hoàn thành
2. ✅ **UserBehaviorController** - Đã hoàn thành
3. ✅ **AppDbContext Configuration** - Đã hoàn thành
4. ✅ **Embedding Service** - Đã hoàn thành
5. ✅ **Recommendation Service** - Đã hoàn thành
6. ✅ **Recommendation Controller** - Đã hoàn thành
7. ✅ **Embedding Controller** - Đã hoàn thành (Admin endpoints)
8. ✅ **Background Job** - Đã hoàn thành (tự động generate embeddings mỗi 6 giờ)
9. ⏳ **Tạo Migration** - Cần chạy migration để tạo tables:
   ```bash
   dotnet ef migrations add AddModelAITables
   dotnet ef database update
   ```
10. ⏳ **Generate Embeddings lần đầu** - Gọi API để generate embeddings:

```bash
POST /api/Embedding/generate-all (Admin only)
```

11. ⏳ **Testing** - Test tất cả endpoints
12. ⏳ **Frontend Integration** - Tích hợp vào frontend
13. ⏳ **AI Model API** - Setup Python FastAPI service để host BERT model (nếu chưa có)

---

## Tài liệu tham khảo

- [Sentence-BERT Paper](https://arxiv.org/abs/1908.10084)
- [Hugging Face Sentence Transformers](https://www.sbert.net/)
- [Cosine Similarity](https://en.wikipedia.org/wiki/Cosine_similarity)
- [ML.NET Documentation](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet)

---

## Liên hệ

Nếu có vấn đề hoặc câu hỏi, vui lòng liên hệ team phát triển.
