# AI Setup & Usage Guide

## 📋 Tổng quan

Hệ thống AI recommendations sử dụng **Content-Based Filtering** với BERT embeddings để gợi ý sản phẩm liên quan. Hướng dẫn này sẽ giúp bạn setup và sử dụng AI trong project.

## 🎯 Kiến trúc AI System

```
┌─────────────────────────────────────────────────────────────┐
│                    AI SYSTEM FLOW                            │
└─────────────────────────────────────────────────────────────┘
1. OFFLINE: Generate Product Embeddings
   → Tất cả sản phẩm được mã hóa bằng BERT
   → Embedding vectors lưu vào ProductEmbeddings table
   
2. REAL-TIME: Track User Behavior
   → User xem/mua/thêm vào giỏ → ghi vào UserBehaviorLogs
   
3. REAL-TIME: Get Recommendations
   → Tính cosine similarity giữa embeddings
   → Trả về Top-K sản phẩm tương đồng nhất
```

## 🚀 Quick Start

### Bước 1: Setup Database

```bash
cd EcomerceBE

# Tạo migration cho AI tables
dotnet ef migrations add AddModelAITables

# Apply migration
dotnet ef database update
```

**Kiểm tra**: Đảm bảo 2 bảng đã được tạo:
- `UserBehaviorLogs`
- `ProductEmbeddings`

### Bước 2: Cấu hình AI Model (Optional)

#### Option A: Sử dụng Python FastAPI Service (Khuyến nghị cho Production)

**Setup Python Service:**

```bash
# Navigate to ModelAI folder
cd ModelAI

# Tạo virtual environment (nếu chưa có)
python -m venv .venv

# Activate virtual environment
# Windows:
.venv\Scripts\activate
# Linux/Mac:
source .venv/bin/activate

# Install dependencies
pip install -r requirements.txt

# Run AI service
python main.py
```

**Service sẽ chạy tại**: `http://localhost:8000`

**Cấu hình Backend:**

Trong `appsettings.json`:
```json
{
  "AIModel": {
    "ApiUrl": "http://localhost:8000"
  }
}
```

#### Option B: Sử dụng Mock Embedding (Development/Testing)

Nếu không có AI Model API, hệ thống sẽ tự động sử dụng mock embedding.

**Không cần cấu hình gì** - hệ thống tự động fallback về mock.

#### Option C: Sử dụng Hugging Face Inference API

Trong `appsettings.json`:
```json
{
  "AIModel": {
    "ApiUrl": "https://api-inference.huggingface.co/models/sentence-transformers/all-MiniLM-L6-v2",
    "ApiKey": "your_huggingface_api_key"
  }
}
```

### Bước 3: Generate Embeddings cho Products

#### Cách 1: Qua API (Admin required)

```bash
# Generate embeddings cho tất cả products chưa có embedding
POST http://localhost:5099/api/Embedding/generate-all
Headers: Authorization: Bearer {admin_jwt_token}
```

#### Cách 2: Tự động qua Background Service

Background service sẽ tự động generate embeddings mỗi 6 giờ cho products mới.

**Không cần làm gì** - service tự động chạy khi ứng dụng khởi động.

#### Cách 3: Generate cho Product cụ thể

```bash
POST http://localhost:5099/api/Embedding/generate/{productId}
Headers: Authorization: Bearer {admin_jwt_token}
```

### Bước 4: Test AI Features

#### Test Embedding Generation

```bash
POST http://localhost:5099/api/Embedding/test
Headers: Authorization: Bearer {admin_jwt_token}
Body: "iPhone 15 Pro Max - Flagship smartphone"
```

#### Test User Behavior Tracking

```bash
# Authenticated user
POST http://localhost:5099/api/UserBehavior/track
Headers: Authorization: Bearer {jwt_token}
Body: {
  "productId": 1,
  "behaviorType": "view",
  "viewDuration": 30
}

# Anonymous user
POST http://localhost:5099/api/UserBehavior/track-anonymous
Body: {
  "productId": 1,
  "behaviorType": "add_to_cart"
}
```

#### Test Recommendations

```bash
# Content-based recommendations
GET http://localhost:5099/api/Recommendation/content-based/1?topK=10

# User-based recommendations (authenticated)
GET http://localhost:5099/api/Recommendation/user-based?topK=10
Headers: Authorization: Bearer {jwt_token}

# Full API
POST http://localhost:5099/api/Recommendation/get-recommendations
Body: {
  "productId": 1,
  "topK": 10,
  "recommendationType": "content_based"
}
```

## 📁 Files liên quan

### Backend Files

- **`Controllers/ModelAI/`**
  - `UserBehaviorController.cs` - Track user behavior
  - `RecommendationController.cs` - Get recommendations
  - `EmbeddingController.cs` - Generate embeddings (Admin)

- **`Service/ModelAI/`**
  - `EmbeddingService.cs` - Generate embeddings từ AI Model API
  - `RecommendationService.cs` - Tính similarity và recommendations

- **`Background/EmbeddingGenerationService.cs`** - Auto-generate embeddings

- **`Models/ModelAI/`**
  - `UserBehaviorLog.cs` - User behavior model
  - `ProductEmbedding.cs` - Product embedding model

### AI Model Files (Python)

- **`ModelAI/main.py`** - FastAPI service để generate embeddings
- **`ModelAI/requirements.txt`** - Python dependencies
- **`ModelAI/README.md`** - AI model documentation
- **`ModelAI/QUICK_START.md`** - Quick start guide
- **`ModelAI/DEPLOYMENT_GUIDE.md`** - Deployment guide

## 🔧 Cấu hình chi tiết

### Python FastAPI Service Setup

**File: `ModelAI/main.py`**

Service này cung cấp endpoint để generate embeddings:

```python
POST /api/embedding/generate
Body: {
  "text": "Product name and description"
}
Response: {
  "embedding": [0.123, 0.456, ...],
  "dimension": 384
}
```

**Requirements:**
- `fastapi`
- `uvicorn`
- `sentence-transformers`
- `torch`

**Chạy service:**
```bash
cd ModelAI
python main.py
# Hoặc với uvicorn:
uvicorn main:app --host 0.0.0.0 --port 8000
```

### Backend Configuration

**`appsettings.json`:**
```json
{
  "AIModel": {
    "ApiUrl": "http://localhost:8000",
    "ApiKey": "" // Optional, nếu AI service cần authentication
  }
}
```

**Nếu không có `AIModel:ApiUrl`**: Hệ thống tự động sử dụng mock embedding.

## 📊 Database Schema

### UserBehaviorLogs Table

Lưu trữ hành vi người dùng:

| Column | Type | Description |
|--------|------|-------------|
| UserBehaviorLogId | INT (PK) | ID tự động |
| UserId | INT (FK, nullable) | User ID (null nếu anonymous) |
| ProductId | INT (FK) | Product ID |
| BehaviorType | VARCHAR(50) | "view", "add_to_cart", "purchase", "wishlist" |
| ViewDuration | INT (nullable) | Thời gian xem (giây) |
| SessionId | VARCHAR(255) | Session ID |
| CreatedAt | DATETIME | Timestamp |

### ProductEmbeddings Table

Lưu trữ embedding vectors:

| Column | Type | Description |
|--------|------|-------------|
| ProductEmbeddingId | INT (PK) | ID tự động |
| ProductId | INT (FK, unique) | Product ID |
| EmbeddingVector | LONGTEXT | JSON array: `[0.123, 0.456, ...]` |
| EmbeddingDimension | INT | Số chiều (384 hoặc 768) |
| ModelName | VARCHAR(255) | Tên model |
| CreatedAt | DATETIME | Timestamp |

## 🎯 Usage Examples

### Frontend Integration

#### Track Product View

```typescript
// Khi user xem product detail
useEffect(() => {
  if (productId) {
    trackUserBehavior({
      productId,
      behaviorType: 'view',
      viewDuration: calculateViewDuration()
    });
  }
}, [productId]);
```

#### Get Recommendations

```typescript
// Content-based recommendations
const { data: recommendations } = useContentBasedRecommendations(productId, 10);

// User-based recommendations
const { data: userRecommendations } = useUserBasedRecommendations(10);
```

### Backend Integration

#### Generate Embeddings

```csharp
// Inject service
private readonly IEmbeddingService _embeddingService;

// Generate for all products
var count = await _embeddingService.GenerateEmbeddingsForAllProductsAsync();

// Generate for single product
await _embeddingService.GenerateEmbeddingForProductAsync(productId);
```

#### Get Recommendations

```csharp
// Inject service
private readonly IRecommendationService _recommendationService;

// Content-based
var recommendations = await _recommendationService
    .GetContentBasedRecommendationsAsync(productId, topK: 10);

// User-based
var userRecommendations = await _recommendationService
    .GetUserBasedRecommendationsAsync(userId, topK: 10);
```

## 🔍 Monitoring & Debugging

### Kiểm tra Embeddings đã được generate

```sql
-- Xem số lượng products đã có embedding
SELECT COUNT(*) FROM ProductEmbeddings;

-- Xem products chưa có embedding
SELECT p.ProductId, p.Name 
FROM Products p
LEFT JOIN ProductEmbeddings pe ON p.ProductId = pe.ProductId
WHERE pe.ProductId IS NULL;
```

### Kiểm tra User Behavior Logs

```sql
-- Xem behavior logs gần đây
SELECT * FROM UserBehaviorLogs 
ORDER BY CreatedAt DESC 
LIMIT 100;

-- Thống kê behavior types
SELECT BehaviorType, COUNT(*) as Count
FROM UserBehaviorLogs
GROUP BY BehaviorType;
```

### Test Recommendations Accuracy

```bash
# Test với product cụ thể
GET /api/Recommendation/content-based/1?topK=10

# Kiểm tra similarity scores trong response
# Scores cao (> 0.7) = recommendations tốt
# Scores thấp (< 0.3) = có thể cần regenerate embeddings
```

## 🐛 Troubleshooting

### Lỗi: "Product embedding not found"

**Nguyên nhân**: Product chưa có embedding.

**Giải pháp**:
```bash
# Generate embedding cho product
POST /api/Embedding/generate/{productId}
```

### Lỗi: "AI Model API connection failed"

**Nguyên nhân**: AI Model API không chạy hoặc URL sai.

**Giải pháp**:
1. Kiểm tra AI service đang chạy: `curl http://localhost:8000/health`
2. Kiểm tra `AIModel:ApiUrl` trong `appsettings.json`
3. Nếu không có AI service, hệ thống sẽ tự động dùng mock embedding

### Recommendations không chính xác

**Nguyên nhân**: 
- Embeddings được generate từ mock (không chính xác)
- Model không phù hợp với domain

**Giải pháp**:
1. Setup Python FastAPI service với real BERT model
2. Regenerate embeddings cho tất cả products
3. Fine-tune model với data của bạn (advanced)

### Background Service không generate embeddings

**Kiểm tra**:
1. Service đã được register trong `Program.cs`:
   ```csharp
   builder.Services.AddHostedService<EmbeddingGenerationService>();
   ```
2. Check logs để xem có errors không
3. Service chạy mỗi 6 giờ, có thể đợi hoặc gọi API thủ công

## 📚 Tài liệu chi tiết

- [ModelAI/README_BACKEND.md](./ModelAI/README_BACKEND.md) - Chi tiết backend implementation
- [ModelAI/README.md](./ModelAI/README.md) - AI model overview
- [ModelAI/QUICK_START.md](./ModelAI/QUICK_START.md) - Quick start guide
- [ModelAI/DEPLOYMENT_GUIDE.md](./ModelAI/DEPLOYMENT_GUIDE.md) - Deployment options
- [Background Services](./README_BACKGROUND_SERVICES.md) - Background services details

## 🎓 Learning Resources

- [Sentence Transformers](https://www.sbert.net/) - BERT embeddings library
- [Cosine Similarity](https://en.wikipedia.org/wiki/Cosine_similarity) - Similarity calculation
- [Content-Based Filtering](https://en.wikipedia.org/wiki/Recommender_system#Content-based_filtering) - Recommendation approach

---

**Last Updated**: 2024-12-17

