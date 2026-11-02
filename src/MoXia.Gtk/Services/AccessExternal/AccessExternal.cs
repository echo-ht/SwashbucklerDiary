using MoXia.Rcl.Essentials;

namespace MoXia.Gtk.Services
{
    public class AccessExternal : Rcl.Services.AccessExternal
    {
        public AccessExternal(IPlatformIntegration platformIntegration,
            IStaticWebAssets staticWebAssets
            ) : base(staticWebAssets, platformIntegration)
        {
        }

        public override Task<bool> OpenAppStoreAppDetails()
        {
            return Task.FromResult(false);
        }

    }
}
