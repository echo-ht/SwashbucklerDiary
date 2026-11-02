using MoXia.Rcl.Essentials;
using MoXia.Shared;

namespace MoXia.Rcl.Services
{
    public interface IGlobalConfiguration
    {
        Dictionary<string, string> WeatherIcons { get; set; }

        Dictionary<string, string> MoodIcons { get; set; }

        Dictionary<AppOperatingSystem, string> AppPlatformIcons { get; set; }

        List<AchievementModel> Achievements { get; set; }

        Task InitializeAsync();
    }
}
