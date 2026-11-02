using Microsoft.AspNetCore.Components;

namespace MoXia.Rcl.Pages
{
    [Route("/export")]
    public class ExportPage : ExportPageBase
    {
        protected override Type LANTransferPageType => typeof(LANTransferPage);
    }
}
