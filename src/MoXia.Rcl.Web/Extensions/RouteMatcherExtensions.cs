using Microsoft.AspNetCore.Components;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Extensions;

namespace MoXia.Rcl.Web.Extensions
{
    public static class RouteMatcherExtensions
    {
        public static bool CheckUrlScheme(this RouteMatcher routeMatcher, NavigationManager navigationManager, string? uriString)
        {
            if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
            {
                var relativePath = navigationManager.ToBaseRelativePath(uri.GetLeftPart(UriPartial.Path));
                var route = $"/{relativePath.TrimEnd('/')}";

                if (routeMatcher.CheckRouter(route))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
