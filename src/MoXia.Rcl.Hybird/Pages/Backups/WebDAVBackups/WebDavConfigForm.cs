using System.ComponentModel.DataAnnotations;

namespace MoXia.Rcl.Pages
{
    public class WebDavConfigForm
    {
        [Required(ErrorMessage = "Please enter the server address")]
        [Url(ErrorMessage = "Please enter the server address")]
        public string? ServerAddress { get; set; }

        [Required(ErrorMessage = "Please enter account")]
        public string? Account { get; set; }

        [Required(ErrorMessage = "Please enter password")]
        public string? Password { get; set; }
    }
}
