using Microsoft.JSInterop;

namespace MoXia.Server.Essentials
{
    public class PlatformIntegrationJSModule : Rcl.Web.Essentials.PlatformIntegrationJSModule
    {
        public PlatformIntegrationJSModule(IJSRuntime js) : base(js, "./js/platformIntegration.js")
        {
        }

        public override ValueTask<string[]?> PickFilesAsync(
            string accept,
            string[] fileExtensions,
            bool multiple = true)
        {
            return InvokeAsync<string[]?>("chooseFiles", accept, fileExtensions, multiple);
        }
    }
}
