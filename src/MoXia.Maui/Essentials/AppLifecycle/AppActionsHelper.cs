using MoXia.Rcl.Essentials;
using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Maui.Essentials
{
    public static class AppActionsHelper
    {
        public static async void HandleAppActions(AppAction action)
        {
            if (action.Id is null) return;

            var activationArguments = await ConvertActivationArguments(action.Id);
            if (activationArguments is null) return;

            AppActivation.LaunchOrActivate(activationArguments);
        }

        public static async Task<ActivationArguments?> ConvertActivationArguments(string id)
        {
            var actions = await AppActions.Current.GetAsync();
            var appAction = actions.FirstOrDefault(a => a.Id == id);
            if (appAction is null) return null;

            return new ActivationArguments()
            {
                Kind = AppActivationKind.Scheme,
                Data = $"{SchemeConstants.MoXia}://{id}"
            };
        }

        public static void SetAppActions(II18nService i18n)
        {
            if (AppActions.Current.IsSupported)
            {
                AppActions.Current.SetAsync([
                   new("write",i18n.T("Add"),icon:"shortcut_pencil"),
                   new("search",i18n.T("Search"),icon:"shortcut_magnify"),
                ]);
            }
        }
    }
}
