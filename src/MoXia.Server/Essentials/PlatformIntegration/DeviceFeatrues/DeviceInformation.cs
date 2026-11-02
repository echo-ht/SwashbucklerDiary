using MoXia.Rcl.Essentials;

namespace MoXia.Server.Essentials
{
    public partial class PlatformIntegration
    {
        public override AppPlatform CurrentPlatform { get; }
            = new BlazorServerAppPlatform();
    }
}