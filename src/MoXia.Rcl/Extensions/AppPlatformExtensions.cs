using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Extensions
{
    public static class AppPlatformExtensions
    {
        public static bool IsInAppStore(this AppPlatform platform)
        {
            return platform is WinUIPackagedAppPlatform
                || (platform.OS == AppOperatingSystem.Android);
        }
    }
}
