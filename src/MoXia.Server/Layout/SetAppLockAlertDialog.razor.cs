namespace MoXia.Server.Layout
{
    public partial class SetAppLockAlertDialog
    {
        private async Task ToSet()
        {
            await InternalVisibleChanged(false);
            To("appLockSetting");
        }
    }
}