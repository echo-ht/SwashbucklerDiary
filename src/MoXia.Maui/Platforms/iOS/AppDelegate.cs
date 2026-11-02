using Foundation;
using MoXia.Maui.Essentials;
using UIKit;

namespace MoXia.Maui
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
        {
            AppActivation.LaunchOrActivate(url);

            return true;
            //return base.OpenUrl(application, url, options);
        }
    }
}