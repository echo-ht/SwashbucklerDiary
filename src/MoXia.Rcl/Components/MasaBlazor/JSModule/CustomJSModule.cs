using Masa.Blazor.JSInterop;
using Microsoft.JSInterop;
using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Components
{
    public class CustomJSModule : JSModule
    {
        public CustomJSModule(IJSRuntime js, string moduleUrl) : base(js, $"./_content/{StaticWebAssets.RclAssemblyName}/{moduleUrl}")
        {
        }
    }
}
