using MoXia.Shared;

namespace MoXia.Rcl.Components
{
    public class DiaryCardListOptions
    {
        public DiaryModel SelectedItemValue { get; set; } = new();

        public string? TimeFormat { get; set; }

        public bool Template { get; set; }

        public Guid? DefaultTemplateId { get; set; }

        public bool Markdown { get; set; }

        public bool AutoTitle { get; set; }
        }
}
