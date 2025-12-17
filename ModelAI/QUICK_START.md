# ModelAI - Quick Start Guide

## 🚀 Bắt đầu nhanh

### Bước 1: Tạo Migration

```bash
# Di chuyển đến thư mục backend
cd EcomerceBE

# Tạo migration
dotnet ef migrations add AddModelAITables

# Áp dụng migration vào database
dotnet ef database update
```

### Bước 2: Generate Embeddings cho tất cả sản phẩm

**Option 1: Gọi API (Admin required)**
```bash
POST http://localhost:5099/api/Embedding/generate-all
Headers: Authorization: Bearer {admin_jwt_token}
```

**Option 2: Đợi Background Job**
- Background service sẽ tự động generate embeddings mỗi 6 giờ
- Hoặc chạy ngay khi ứng dụng khởi động

### Bước 3: Test APIs

#### 3.1. Track User Behavior
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

#### 3.2. Get Recommendations
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

#### 3.3. Admin: Generate Embeddings
```bash
# Generate for all products
POST http://localhost:5099/api/Embedding/generate-all
Headers: Authorization: Bearer {admin_jwt_token}

# Generate for single product
POST http://localhost:5099/api/Embedding/generate/1
Headers: Authorization: Bearer {admin_jwt_token}

# Test embedding generation
POST http://localhost:5099/api/Embedding/test
Headers: Authorization: Bearer {admin_jwt_token}
Body: "iPhone 15 Pro Max - Flagship smartphone"
```

## 📋 Checklist

- [ ] Đã tạo migration và apply vào database
- [ ] Đã generate embeddings cho tất cả sản phẩm
- [ ] Đã test track user behavior API
- [ ] Đã test recommendations API
- [ ] Đã tích hợp vào frontend (nếu có)

## 🔧 Cấu hình AI Model API (Optional)

Nếu bạn có Python FastAPI service để host BERT model, thêm vào `appsettings.json`:

```json
{
  "AIModel": {
    "ApiUrl": "http://localhost:8000"
  }
}
```

Nếu không có, hệ thống sẽ sử dụng mock embedding (để test).

## 📚 Tài liệu chi tiết

Xem `README_BACKEND.md` để biết thêm chi tiết về:
- Kiến trúc hệ thống
- Database schema
- API endpoints đầy đủ
- Implementation guide
- Troubleshooting

