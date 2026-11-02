using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Web.Essentials
{
    public partial class PlatformIntegration
    {
        public Task<string?> PickZipFileAsync()
            => PickFileAsync(PlatformIntegrationHelper.ZipMimeTypes, PlatformIntegrationHelper.ZipFileExtensions);
    }
}
