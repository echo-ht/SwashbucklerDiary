using Masa.Blazor;
using Microsoft.AspNetCore.Components.Web;

namespace MoXia.Rcl.Components
{
    public class CustomMTextField<TValue> : MTextField<TValue>
    {
        public override async Task HandleOnClearClickAsync(MouseEventArgs args)
        {
            await base.HandleOnClearClickAsync(args);

            if (OnInput.HasDelegate)
            {
                await OnInput.InvokeAsync(default);
            }
        }
    }
}
