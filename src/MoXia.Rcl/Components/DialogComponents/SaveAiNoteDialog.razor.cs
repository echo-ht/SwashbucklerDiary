using Microsoft.AspNetCore.Components;

namespace MoXia.Rcl.Components
{
    public partial class SaveAiNoteDialog : DialogComponentBase
    {
        private string title = string.Empty;
        private string content = string.Empty;
        private bool wasVisible;

        [Parameter]
        public string? InitialTitle { get; set; }

        [Parameter]
        public string? InitialContent { get; set; }

        [Parameter]
        public bool Saving { get; set; }

        [Parameter]
        public EventCallback<(string Title, string Content)> OnSave { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (Visible && !wasVisible)
            {
                title = InitialTitle ?? string.Empty;
                content = InitialContent ?? string.Empty;
            }

            wasVisible = Visible;
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(content) || !OnSave.HasDelegate)
            {
                return;
            }

            await OnSave.InvokeAsync((title.Trim(), content.Trim()));
        }
    }
}
