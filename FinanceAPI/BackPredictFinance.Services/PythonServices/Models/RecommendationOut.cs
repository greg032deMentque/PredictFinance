using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public class RecommendationOut
    {
        public string Symbol { get; set; } = "";
        public DateTime RecommendedAt { get; set; }
        public string Action { get; set; } = "";
        public decimal Confidence { get; set; }
        public decimal? TargetPrice { get; set; }
        public string? Reason { get; set; }
    }
}
