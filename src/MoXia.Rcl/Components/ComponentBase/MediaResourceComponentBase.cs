using Microsoft.AspNetCore.Components;
using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Rcl.Components
{
    public abstract class MediaResourceComponentBase : CardComponentBase<ResourceModel>
    {
        protected MediaResourcePath? mediaResourcePath;

        [Inject]
        protected IMediaResourceManager MediaResourceManager { get; set; } = default!;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            mediaResourcePath = MediaResourceManager.ToMediaResourcePath(NavigationManager, Value.ResourceUri);
        }
    }
}
