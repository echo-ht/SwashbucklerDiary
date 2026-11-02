using MoXia.Rcl.Components;

namespace MoXia.Rcl.Pages
{
    public partial class IndexSetting : ImportantComponentBase
    {
        private bool welcomText;

        private bool date;

        protected override void ReadSettings()
        {
            welcomText = SettingService.Get(s => s.WelcomeText);
            date = SettingService.Get(s => s.IndexDate);
        }
    }
}
