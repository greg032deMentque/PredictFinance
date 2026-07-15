using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.Common
{
    public class FrontendOptions
    {
        [Required(ErrorMessage = "Frontend:BaseUrl is required")]
        public string BaseUrl { get; set; } = string.Empty;
    }
}
