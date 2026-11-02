using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MoXia.Rcl.Components;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Extensions;

namespace MoXia.Rcl.Web.Essentials
{
    [Route($"/{NavigateControllerHelper.StackBottomRelativePath}")]
    public partial class StackBottomPage : ImportantComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        protected override void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            base.NavigationManagerOnLocationChanged(sender, e);

            InvokeAsync(StateHasChanged);
        }

        private async Task HandleOnCancle()
        {
            await JSRuntime.HistoryGo(1);
        }
    }
}