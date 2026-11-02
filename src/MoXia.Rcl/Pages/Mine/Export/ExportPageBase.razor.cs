using Masa.Blazor;
using MoXia.Rcl.Components;
using MoXia.Rcl.Models;

namespace MoXia.Rcl.Pages
{
    public abstract partial class ExportPageBase : ImportantComponentBase
    {
        protected readonly List<TabListItem> tabListItems =
        [
            new("Local export","local"),
            new("LAN","LAN"),
        ];

        protected StringNumber tab = 0;

        protected abstract Type LANTransferPageType { get; }
    }
}
