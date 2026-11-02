using Microsoft.Maui.Storage;

namespace MoXia.Server.Essentials
{
    public class AppFileSystem : Rcl.Essentials.AppFileSystem
    {
        public override string AppDataDirectory => FileSystem.AppDataDirectory;

        public override string CacheDirectory => FileSystem.CacheDirectory;
    }
}
