using MoXia.Rcl.Components;
using MoXia.Rcl.Extensions;

namespace MoXia.Rcl.Pages
{
    public partial class PrivacyPolicyPage : ImportantComponentBase
    {
        private string? content;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            content = await StaticWebAssets.ReadRclI18nTextAsync("docs/privacy-policy/{0}.md", I18n.Culture);
        }
    }
}
