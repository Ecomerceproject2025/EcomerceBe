from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import uvicorn
 
app = FastAPI(title="Ecommerce Embedding Service")
 
# Load Sentence-BERT model at startup
model = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")
EMBEDDING_DIM = model.get_sentence_embedding_dimension()
 
 
class EmbeddingRequest(BaseModel):
    text: str
 
 
class EmbeddingResponse(BaseModel):
    embedding: list[float]
    dimension: int
 
 
@app.post("/api/embedding/generate", response_model=EmbeddingResponse)
def generate_embedding(req: EmbeddingRequest):
    text = req.text.strip()
    if not text:
        return EmbeddingResponse(embedding=[], dimension=0)
 
    vector = model.encode(text, normalize_embeddings=True)
    return EmbeddingResponse(
        embedding=vector.tolist(),
        dimension=len(vector),
    )
 
 
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)