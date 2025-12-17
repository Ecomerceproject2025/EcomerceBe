namespace EcomerceBE.Service.ModelAI
{
    /// <summary>
    /// Service để generate embedding vectors từ text sử dụng AI model (BERT)
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generate embedding vector từ text (tên sản phẩm + mô tả)
        /// </summary>
        /// <param name="text">Text cần mã hóa (ví dụ: "iPhone 15 Pro Max - Flagship smartphone with A17 chip")</param>
        /// <returns>Embedding vector dưới dạng array of floats</returns>
        Task<float[]> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Generate embeddings cho tất cả sản phẩm chưa có embedding
        /// </summary>
        Task<int> GenerateEmbeddingsForAllProductsAsync();

        /// <summary>
        /// Update embedding cho một sản phẩm cụ thể (khi sản phẩm được cập nhật)
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        Task UpdateEmbeddingForProductAsync(int productId);
    }
}

