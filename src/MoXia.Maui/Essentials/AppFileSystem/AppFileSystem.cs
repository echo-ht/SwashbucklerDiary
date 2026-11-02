using MoXia.Rcl.Essentials;

namespace MoXia.Maui.Essentials
{
    public partial class AppFileSystem : Rcl.Essentials.AppFileSystem
    {
        private static IAppFileSystem? defaultImplementation;

        public static IAppFileSystem Default
            => defaultImplementation ??= new AppFileSystem();

        public override string AppDataDirectory =>
#if IOS || MACCATALYST
            _platformAppDataDirectory.Value;
#else
            FileSystem.Current.AppDataDirectory;
#endif

        public override string CacheDirectory =>
#if IOS || MACCATALYST
            _platformCacheDirectory.Value;
#else
            FileSystem.Current.CacheDirectory;
#endif
    }
}
