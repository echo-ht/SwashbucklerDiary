using Microsoft.Maui.Storage;

namespace MoXia.Gtk.Essentials
{
    public class AppFileSystem : Rcl.Essentials.AppFileSystem
    {
        public override string AppDataDirectory => FileSystem.AppDataDirectory;

        public override string CacheDirectory => FileSystem.CacheDirectory;
    }
}
