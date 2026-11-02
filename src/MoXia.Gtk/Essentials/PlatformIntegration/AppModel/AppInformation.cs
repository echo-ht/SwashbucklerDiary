using Microsoft.Maui.ApplicationModel;

namespace MoXia.Gtk.Essentials
{
    public partial class PlatformIntegration
    {
        public string AppVersionString => AppInfo.VersionString;

        public Task ShowSettingsUI()
        {
            return Task.CompletedTask;
        }
    }
}
