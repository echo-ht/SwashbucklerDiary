using Microsoft.JSInterop;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Extensions;

namespace MoXia.Rcl.Web.Services
{
    public class AccessExternal : Rcl.Services.AccessExternal
    {
        private readonly Lazy<ValueTask<IJSObjectReference>> _module;

        private readonly Lazy<Task<Dictionary<string, string>>> _appIds;

        private Task<Dictionary<string, string>> AppIds => _appIds.Value;

        private ValueTask<IJSObjectReference> Module => _module.Value;

        public AccessExternal(IPlatformIntegration platformIntegration,
            IStaticWebAssets staticWebAssets,
            IJSRuntime jS
            ) : base(staticWebAssets, platformIntegration)
        {
            _appIds = new(() => _staticWebAssets.ReadRclJsonAsync<Dictionary<string, string>>("json/app-id/app-id.json"));
            _module = new(() => jS.ImportJsModule("/_content/MoXia.Rcl.Web/js/accessExternal.js"));
        }

        public override async Task<bool> OpenAppStoreAppDetails()
        {
            var appIds = await AppIds.ConfigureAwait(false);
            var module = await Module.ConfigureAwait(false);
            return await module.InvokeAsync<bool>("openAppStoreAppDetails", appIds).ConfigureAwait(false);
        }

    }
}
