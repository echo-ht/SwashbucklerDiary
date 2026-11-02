using Microsoft.AspNetCore.Components;
using MoXia.Rcl.Models;

namespace MoXia.Rcl.Components
{
    public partial class MultiSizeListDialog : DialogComponentBase
    {
        [Parameter]
        public string? Title { get; set; }

        [EditorRequired]
        [Parameter]
        public List<DynamicListItem> DynamicListItems { get; set; } = [];
    }
}
