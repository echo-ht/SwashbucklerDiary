namespace MoXia.Shared
{
    public class TagModel : BaseModel
    {
        public string? Name { get; set; }

        public List<DiaryModel>? Diaries { get; set; }
    }
}
