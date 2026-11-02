using Masa.Blazor;
using MoXia.Rcl.Components;
using MoXia.Rcl.Models;
using MoXia.Shared;

namespace MoXia.Rcl.Pages
{
    public partial class UrlSchemePage : ImportantComponentBase
    {
        private List<UrlSchemePath> urlSchemePaths = [];

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            await ReadJson();
        }

        protected override void ReadSettings()
        {
            base.ReadSettings();

            UrlScheme = SettingService.Get(s => s.UrlScheme);
        }

        protected override void RegisterWatchers(PropertyWatcher watcher)
        {
            base.RegisterWatchers(watcher);

            watcher.Watch<StringNumber>(nameof(UrlScheme), async () =>
            {
                await SettingService.SetAsync(s => s.UrlScheme, UrlScheme.ToString());
            });
        }

        private StringNumber UrlScheme
        {
            get => GetValue<StringNumber>() ?? SchemeConstants.MoXia;
            set => SetValue(value);
        }

        private async Task ReadJson()
        {
            urlSchemePaths = await StaticWebAssets.ReadRclJsonAsync<List<UrlSchemePath>>("json/url-scheme/url-scheme-paths.json");
        }

        private async Task CopyAsync(string path)
        {
            var text = $"{UrlScheme}://{path}";
            await PlatformIntegration.SetClipboardAsync(text);
            await AlertService.SuccessAsync(I18n.T("Copy successfully"));
        }
    }
}