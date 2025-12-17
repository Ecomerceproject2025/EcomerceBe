# ModelAI - Hướng dẫn Deploy AI Model

## 🎯 Tổng quan các phương án

Bạn **KHÔNG BẮT BUỘC** phải deploy AI model ở server riêng. Có 4 phương án:

---

## Phương án 1: Deploy AI Model ở Server riêng (Python FastAPI) ⭐ Recommended cho Production

### Ưu điểm:
- ✅ Tách biệt concerns (Backend .NET và AI Model riêng)
- ✅ Dễ scale AI model độc lập
- ✅ Dễ update model mà không ảnh hưởng backend
- ✅ Có thể dùng GPU server cho AI model

### Nhược điểm:
- ❌ Cần thêm 1 server
- ❌ Phức tạp hơn về infrastructure

### Cách setup:

#### 1. Tạo Python FastAPI Service

**File: `ai_model_service/main.py`**
```python
from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import uvicorn

app = FastAPI()

# Load model khi khởi động
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')

class EmbeddingRequest(BaseModel):
    text: str

class EmbeddingResponse(BaseModel):
    embedding: list[float]
    dimension: int

@app.post("/api/embedding/generate", response_model=EmbeddingResponse)
async def generate_embedding(request: EmbeddingRequest):
    # Generate embedding
    embedding = model.encode(request.text, normalize_embeddings=True)
    
    return EmbeddingResponse(
        embedding=embedding.tolist(),
        dimension=len(embedding)
    )

@app.get("/health")
async def health():
    return {"status": "ok"}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
```

#### 2. Tạo requirements.txt
```txt
fastapi==0.104.1
uvicorn==0.24.0
sentence-transformers==2.2.2
torch==2.1.0
```

#### 3. Deploy lên server
```bash
# Install dependencies
pip install -r requirements.txt

# Run service
python main.py
# Hoặc dùng gunicorn cho production
gunicorn main:app -w 4 -k uvicorn.workers.UvicornWorker --bind 0.0.0.0:8000
```

#### 4. Cấu hình Backend (.NET)

Trong `appsettings.json`:
```json
{
  "AIModel": {
    "ApiUrl": "http://your-ai-server:8000"
  }
}
```

#### 5. Test
```bash
# Test AI Model API
curl -X POST http://localhost:8000/api/embedding/generate \
  -H "Content-Type: application/json" \
  -d '{"text": "iPhone 15 Pro Max"}'

# Test từ Backend
POST http://localhost:5099/api/Embedding/test
Body: "iPhone 15 Pro Max"
```

---

## Phương án 2: Sử dụng ML.NET với ONNX Model (Chạy trong .NET) 🚀 Không cần server riêng

### Ưu điểm:
- ✅ Không cần server riêng
- ✅ Tất cả chạy trong 1 ứng dụng .NET
- ✅ Không có network latency

### Nhược điểm:
- ❌ Cần convert BERT model sang ONNX
- ❌ Tốn RAM/CPU của backend server
- ❌ Phức tạp hơn về implementation

### Cách setup:

#### 1. Convert BERT model sang ONNX
```python
# convert_to_onnx.py
from transformers import AutoTokenizer, AutoModel
import torch

model_name = "sentence-transformers/all-MiniLM-L6-v2"
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModel.from_pretrained(model_name)

# Export to ONNX
dummy_input = tokenizer("test", return_tensors="pt")
torch.onnx.export(
    model,
    (dummy_input["input_ids"], dummy_input["attention_mask"]),
    "bert_model.onnx",
    input_names=["input_ids", "attention_mask"],
    output_names=["last_hidden_state"],
    dynamic_axes={
        "input_ids": {0: "batch", 1: "sequence"},
        "attention_mask": {0: "batch", 1: "sequence"}
    }
)
```

#### 2. Install ML.NET packages
```bash
dotnet add package Microsoft.ML
dotnet add package Microsoft.ML.OnnxRuntime
```

#### 3. Implement trong EmbeddingService
```csharp
// Sử dụng ONNX Runtime để load và chạy model
// (Code phức tạp hơn, cần implement tokenization và model inference)
```

**Lưu ý:** Phương án này phức tạp và cần nhiều code hơn.

---

## Phương án 3: Sử dụng Hugging Face Inference API 🌐 Đơn giản nhất

### Ưu điểm:
- ✅ Không cần server riêng
- ✅ Không cần maintain model
- ✅ Đơn giản nhất

### Nhược điểm:
- ❌ Phụ thuộc vào Hugging Face
- ❌ Có thể tốn phí nếu dùng nhiều
- ❌ Có rate limit

### Cách setup:

#### 1. Đăng ký Hugging Face API Key
- Truy cập: https://huggingface.co/settings/tokens
- Tạo API token

#### 2. Cấu hình Backend
Trong `appsettings.json`:
```json
{
  "AIModel": {
    "ApiUrl": "https://api-inference.huggingface.co/models/sentence-transformers/all-MiniLM-L6-v2",
    "ApiKey": "your_huggingface_api_key"
  }
}
```

#### 3. Update EmbeddingService
```csharp
// Thêm header Authorization với API key
_httpClient.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
```

---

## Phương án 4: Mock Embedding (Hiện tại) 🧪 Chỉ để Test

### Ưu điểm:
- ✅ Đơn giản nhất
- ✅ Không cần setup gì

### Nhược điểm:
- ❌ Không phải embedding thật
- ❌ Recommendations không chính xác
- ❌ Chỉ dùng để test

### Hiện tại:
Code đã có sẵn mock embedding trong `EmbeddingService.cs`. Nếu không có `AIModel:ApiUrl`, sẽ tự động dùng mock.

---

## 🎯 Khuyến nghị

### Cho Development/Testing:
- **Dùng Mock Embedding** (như hiện tại) - Đơn giản, đủ để test flow

### Cho Production nhỏ (< 1000 sản phẩm):
- **Dùng Hugging Face Inference API** - Đơn giản, không cần maintain

### Cho Production lớn (> 1000 sản phẩm):
- **Deploy Python FastAPI riêng** - Tốt nhất về performance và cost

### Cho Production với yêu cầu đặc biệt:
- **ML.NET với ONNX** - Nếu muốn tất cả trong .NET, không phụ thuộc external

---

## 📝 Quick Start với Python FastAPI (Recommended)

### Bước 1: Tạo Python service
```bash
mkdir ai_model_service
cd ai_model_service

# Tạo file main.py (như trên)
# Tạo file requirements.txt (như trên)

# Install
pip install -r requirements.txt
```

### Bước 2: Run service
```bash
python main.py
# Service chạy ở http://localhost:8000
```

### Bước 3: Cấu hình Backend
```json
{
  "AIModel": {
    "ApiUrl": "http://localhost:8000"
  }
}
```

### Bước 4: Test
```bash
# Test AI service
curl -X POST http://localhost:8000/api/embedding/generate \
  -H "Content-Type: application/json" \
  -d '{"text": "iPhone 15 Pro Max"}'

# Test từ Backend
POST http://localhost:5099/api/Embedding/test
Body: "iPhone 15 Pro Max"
```

### Bước 5: Deploy lên server
```bash
# Dùng gunicorn cho production
pip install gunicorn
gunicorn main:app -w 4 -k uvicorn.workers.UvicornWorker --bind 0.0.0.0:8000

# Hoặc dùng Docker
# (Tạo Dockerfile)
```

---

## 🔧 Deploy lên Cloud

### Option 1: Deploy Python service lên cùng server với Backend
- Chạy Python service ở port 8000
- Backend gọi `http://localhost:8000`

### Option 2: Deploy Python service lên server riêng
- Deploy lên server khác (có thể dùng GPU)
- Backend gọi qua IP/domain

### Option 3: Deploy lên Cloud (AWS, Azure, GCP)
- Deploy Python service lên cloud function hoặc container
- Backend gọi qua cloud endpoint

---

## ❓ FAQ

**Q: Có thể dùng mock embedding trong production không?**
A: Không nên. Mock embedding không chính xác, recommendations sẽ không tốt.

**Q: Nếu không có server riêng, dùng gì?**
A: Dùng Hugging Face Inference API (Phương án 3) - đơn giản nhất.

**Q: Python service có cần GPU không?**
A: Không bắt buộc. CPU cũng chạy được, nhưng GPU sẽ nhanh hơn nhiều.

**Q: Có thể deploy Python service và Backend cùng 1 server không?**
A: Có, chạy Python ở port 8000, Backend ở port 5099.

---

## 📚 Tài liệu tham khảo

- [Sentence Transformers](https://www.sbert.net/)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Hugging Face Inference API](https://huggingface.co/docs/api-inference/index)
- [ML.NET ONNX](https://learn.microsoft.com/en-us/dotnet/machine-learning/)

