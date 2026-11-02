using Microsoft.AspNetCore.Components;
using MoXia.Rcl.Pages;

namespace MoXia.Rcl.Web.Pages
{
    [Route("/export")]
    public class ExportPage : ExportPageBase
    {
        protected override Type LANTransferPageType => typeof(LANTransferPage);
    }
}
