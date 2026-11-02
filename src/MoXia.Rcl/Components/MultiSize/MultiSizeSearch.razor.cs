using Microsoft.AspNetCore.Components;

namespace MoXia.Rcl.Components
{
    public partial class MultiSizeSearch
    {
        private string? searchText;

        [Parameter]
        public EventCallback<string> OnSearch { get; set; }

        private async Task Search()
        {
            if (OnSearch.HasDelegate)
            {
                await OnSearch.InvokeAsync(searchText);
            }

            searchText = string.Empty;
        }
    }
}
