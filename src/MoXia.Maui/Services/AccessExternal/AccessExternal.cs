using MauiBlazorToolkit.Essentials;
using MoXia.Rcl.Essentials;

namespace MoXia.Maui.Services
{
    public class AccessExternal : Rcl.Services.AccessExternal
    {
        private readonly Lazy<string> _appId;

        private string AppId => _appId.Value;

        public AccessExternal(IPlatformIntegration platformIntegration,
            IStaticWebAssets staticWebAssets
            ) : base(staticWebAssets, platformIntegration)
        {
            //读取应用商店的AppId
            //目前只有Windows和Android
#if WINDOWS || ANDROID
            _appId = new(() =>
            {
                var appIds = _staticWebAssets.ReadRclJsonAsync<Dictionary<AppPlatform, string>>("json/app-id/app-id.json").Result;
                return appIds[_platformIntegration.CurrentPlatform];
            });
#else
            _appId = new(() => string.Empty);
#endif
        }

        public override Task<bool> OpenAppStoreAppDetails()
        {
#if IOS || MACCATALYST
            return Task.FromResult(false);
#else

#if WINDOWS
            if (_platformIntegration.CurrentPlatform is WinUIUnpackagedAppPlatform)
            {
                return Task.FromResult(false);
            }
#endif
            return AppStoreLauncher.Default.TryOpenAsync(AppId);
#endif
        }


    }
}
