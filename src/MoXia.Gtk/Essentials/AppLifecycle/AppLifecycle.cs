using MoXia.Rcl.Essentials;
using Application = Gtk.Application;

namespace MoXia.Gtk.Essentials
{
    public class AppLifecycle : Rcl.Essentials.AppLifecycle
    {
        private static IAppLifecycle? defaultImplementation;

        public static IAppLifecycle Default
            => defaultImplementation ??= new AppLifecycle();

        public override void QuitApp()
        {
            Application.GetDefault()?.Quit();
        }
    }
}
