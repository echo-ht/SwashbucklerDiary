using Masa.Blazor;
using MoXia.Rcl.Components;
using MoXia.Rcl.Models;

namespace MoXia.Rcl.Pages
{
    public abstract partial class BackupsPageBase : ImportantComponentBase
    {
        protected readonly List<TabListItem> tabListItems =
        [
            new("Local backup","local"),
            new("WebDAV","webDAV"),
        ];

        protected StringNumber tab = 0;

        protected abstract Type WebDAVBackupsPageType { get; }
    }
}
