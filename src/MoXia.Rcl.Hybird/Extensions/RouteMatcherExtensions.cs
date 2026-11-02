using MoXia.Rcl.Essentials;
using MoXia.Rcl.Extensions;

namespace MoXia.Rcl.Hybird.Extensions
{
    public static class RouteMatcherExtensions
    {
        public static bool CheckUrlScheme(this RouteMatcher routeMatcher, string? uriString, out string path)
        {
            if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
            {
                var route = $"/{uri.Host}{uri.AbsolutePath.TrimEnd('/')}";

                if (routeMatcher.CheckRouter(route))
                {
                    path = $"{route}{uri.Query}{uri.Fragment}";
                    return true;
                }
            }

            path = string.Empty;
            return false;
        }
    }
}
