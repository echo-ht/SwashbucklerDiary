using MoXia.Rcl.Essentials;
using MoXia.Rcl.Services;

namespace MoXia.Gtk.Services
{
    public class SettingService : Rcl.Services.SettingService, ISettingService
    {
        public SettingService(IPreferences preferences) : base(preferences)
        {
        }

        public override T Get<T>(string key, T defaultValue)
        {
            return Microsoft.Maui.Storage.Preferences.Default.Get(key, defaultValue);
        }
    }
}
