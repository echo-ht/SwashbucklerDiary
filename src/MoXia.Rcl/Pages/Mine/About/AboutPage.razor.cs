using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MoXia.Rcl.Components;
using MoXia.Rcl.Models;
using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Rcl.Pages
{
    public partial class AboutPage : ImportantComponentBase
    {
        private readonly string recordNumber = "辽ICP备2023009191号-2A";

        private bool showSourceCode;

        private List<DynamicListItem> codeSourceListItems = [];

        private List<List<DynamicListItem>> viewLists = [];

        [Inject]
        private IAccessExternal AccessExternal { get; set; } = default!;

        [Inject]
        private ILogger<AboutPage> Logger { get; set; } = default!;

        [Inject]
        private IVersionUpdataManager VersionUpdataManager { get; set; } = default!;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LoadView();
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            await LoadViewAsync();
        }

        private string AppVersion => PlatformIntegration.AppVersionString;

        private void LoadView()
        {
            viewLists =
            [
                [
                    new(this, "View source","auto_stories",() => showSourceCode = true),
                    new(this, "User agreement","mdi:mdi-file-document-multiple-outline",() => To("user-agreement")),
                    new(this, "Privacy policy","lock",() => To("privacy-policy")),
                    new(this, "Check for updates","update",CheckForUpdates),
                ],
                [
                    new(this, "Open source related","mdi:mdi-xml",() => To("relatedOSP")),
                    new(this, "Log file","mdi:mdi-file-document-edit-outline",() => To("log")),
                    new(this, "Give a good review","star",OpenAppDetails),
                ]
            ];
        }

        private async Task LoadViewAsync()
        {
            var codeSources = await StaticWebAssets.ReadRclJsonAsync<List<CodeSource>>("json/code-source/code-source.json");
            codeSourceListItems = codeSources.Select(it => new DynamicListItem(this, it.Name ?? string.Empty, it.Icon ?? string.Empty, () => ViewSourceCode(it.Url))).ToList();
        }

        private async Task ViewSourceCode(string? url)
        {
            await PlatformIntegration.OpenBrowser(url);
            await HandleAchievements(Achievement.SourceCode);
        }

        private async Task OpenAppDetails()
        {
            bool flag = await AccessExternal.OpenAppStoreAppDetails();
            if (!flag)
            {
                await AlertService.ErrorAsync(I18n.T("Failed to open the application store"));
            }
        }

        private void CheckForUpdates()
        {
            VersionUpdataManager.CheckUpdates();
        }

        private async Task CopyRecordNumber()
        {
            await PlatformIntegration.SetClipboardAsync(recordNumber);
            await AlertService.SuccessAsync(I18n.T("Copy successfully"));
        }
    }
}
