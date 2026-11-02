using Microsoft.AspNetCore.Components;
using MoXia.Rcl.Models;
using MoXia.Rcl.Services;

namespace MoXia.Rcl.Layout
{
    public abstract class MainLayoutNavBase : ComponentBase, IDisposable
    {
        [Inject]
        protected BreakpointService BreakpointService { get; set; } = default!;

        [Inject]
        protected II18nService I18n { get; set; } = default!;

        [CascadingParameter(Name = "Culture")]
        public string? Culture { get; set; }

        [Parameter]
        public List<NavigationButton> Items { get; set; } = [];

        [Parameter]
        public bool IsPermanentPath { get; set; }

        public void Dispose()
        {
            BreakpointService.BreakpointChanged -= HandleBreakpointChange;
            GC.SuppressFinalize(this);
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            BreakpointService.BreakpointChanged += HandleBreakpointChange;
        }

        private void HandleBreakpointChange(object? sender, MyBreakpointChangedEventArgs e)
        {
            if (!e.XsChanged)
            {
                return;
            }

            StateHasChanged();
        }
    }
}
