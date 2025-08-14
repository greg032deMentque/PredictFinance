using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public class PredictOut
    {
        public string Symbol { get; set; } = "";
        public DateTime PredictedAt { get; set; }
        public List<PatternPrediction> Patterns { get; set; } = new();
    }
}
