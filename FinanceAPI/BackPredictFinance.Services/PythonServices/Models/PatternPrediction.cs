using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public class PatternPrediction
    {
        public string Pattern { get; set; } = "";
        public decimal Probability { get; set; }
    }
}
