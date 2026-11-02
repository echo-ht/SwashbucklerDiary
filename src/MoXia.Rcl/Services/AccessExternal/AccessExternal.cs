using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Services
{
    public abstract class AccessExternal : IAccessExternal
    {
        protected readonly IStaticWebAssets _staticWebAssets;

        protected readonly IPlatformIntegration _platformIntegration;

        public AccessExternal(IStaticWebAssets staticWebAssets,
            IPlatformIntegration platformIntegration)
        {
            _staticWebAssets = staticWebAssets;
            _platformIntegration = platformIntegration;
        }

        public abstract Task<bool> OpenAppStoreAppDetails();
    }
}
