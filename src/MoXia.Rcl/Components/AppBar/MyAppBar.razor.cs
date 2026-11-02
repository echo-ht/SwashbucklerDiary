using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MoXia.Rcl.Components
{
    public partial class MyAppBar
    {
        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}
