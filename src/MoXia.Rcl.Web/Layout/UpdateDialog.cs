using Microsoft.AspNetCore.Components;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Layout;

namespace MoXia.Rcl.Web.Layout
{
    public class UpdateDialog : UpdateDialogBase
    {
        private const string githubUrl = "https://github.com/echo-ht/SwashbucklerDiary";

        [Inject]
        private IPlatformIntegration PlatformIntegration { get; set; } = default!;

        protected override async Task ToUpdate()
        {
            await PlatformIntegration.OpenBrowser(githubUrl);
        }
    }
}
