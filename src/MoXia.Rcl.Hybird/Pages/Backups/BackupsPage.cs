using Microsoft.AspNetCore.Components;

namespace MoXia.Rcl.Pages
{
    [Route("/backups")]
    public class BackupsPage : BackupsPageBase
    {
        protected override Type WebDAVBackupsPageType => typeof(WebDAVBackupsPage);
    }
}
