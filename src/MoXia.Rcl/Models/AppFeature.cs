using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Models
{
    public class AppFeature
    {
        public string? Name { get; set; }

        public string? Icon { get; set; }

        public string? Path { get; set; }

        public string? Href { get; set; }

        public AppPlatform[]? DisplayPlatforms { get; set; }

        public AppPlatform[]? HidePlatforms { get; set; }

        public string[]? HideBreakpoints { get; set; }
    }
}
