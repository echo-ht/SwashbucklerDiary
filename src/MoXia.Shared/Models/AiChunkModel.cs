namespace MoXia.Shared
{
    public class AiChunkModel : BaseModel
    {
        public Guid DiaryId { get; set; }

        public string? DiaryTitle { get; set; }

        public int ChunkIndex { get; set; }

        public string Text { get; set; } = string.Empty;

        public string EmbeddingJson { get; set; } = "[]";

        public string EmbeddingSignature { get; set; } = string.Empty;

        public string? ContentHash { get; set; }

        public DateTime DiaryUpdateTime { get; set; }
    }
}
