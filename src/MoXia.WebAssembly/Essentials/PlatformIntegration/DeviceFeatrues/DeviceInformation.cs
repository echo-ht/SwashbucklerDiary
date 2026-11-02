using MoXia.Rcl.Essentials;

namespace MoXia.WebAssembly.Essentials
{
    public partial class PlatformIntegration
    {
        public override AppPlatform CurrentPlatform { get; }
            = new BlazorWebAssemblyAppPlatform();
    }
}
