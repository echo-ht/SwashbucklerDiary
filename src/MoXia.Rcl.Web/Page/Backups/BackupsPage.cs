using Microsoft.AspNetCore.Components;
using MoXia.Rcl.Pages;

namespace MoXia.Rcl.Web.Pages
{
    [Route("/backups")]
    public class BackupsPage : BackupsPageBase
    {
        protected override Type WebDAVBackupsPageType => typeof(WebDAVBackupsPage);
    }
}
