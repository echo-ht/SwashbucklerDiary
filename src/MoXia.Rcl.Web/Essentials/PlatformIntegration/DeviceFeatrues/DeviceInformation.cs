using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Web.Essentials
{
    public partial class PlatformIntegration
    {
        public abstract AppPlatform CurrentPlatform { get; }

        public string DeviceName => string.Empty;
    }
}
