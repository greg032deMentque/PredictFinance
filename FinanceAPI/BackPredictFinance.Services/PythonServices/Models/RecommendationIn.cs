using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public class RecommendationIn
    {
        public string Symbol { get; set; } = "";
        public string Action { get; set; } = "";  // buy/sell/hold
        public decimal Confidence { get; set; }
    }
}
