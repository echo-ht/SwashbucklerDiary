using Android.App;
using Android.Widget;
using MoXia.Rcl;
using MoXia.Rcl.Essentials;
using static Android.Resource;

namespace MoXia.Maui
{
    public static class SoftKeyboardAdjustResizeHelper
    {
        static readonly Android.Graphics.Color lightColor = Android.Graphics.Color.ParseColor(ThemeColor.LightSurface);

        static readonly Android.Graphics.Color darkColor = Android.Graphics.Color.ParseColor(ThemeColor.DarkSurface);

        public static void InitBackgroundColor(Activity activity)
        {
            var themeService = IPlatformApplication.Current.Services.GetRequiredService<IThemeService>();
            var mChildOfContent = activity.FindViewById<FrameLayout>(Id.Content)?.GetChildAt(0);
            HandleThemeChanged(themeService.RealTheme);
            themeService.ThemeChanged += HandleThemeChanged;

            void HandleThemeChanged(Shared.Theme theme)
            {
                var color = theme == Shared.Theme.Dark ? darkColor : lightColor;
                mChildOfContent?.RootView?.SetBackgroundColor(color);
            }
        }
    }
}
