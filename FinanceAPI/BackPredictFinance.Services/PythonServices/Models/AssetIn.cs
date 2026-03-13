using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public class AssetIn
    {
        public string Symbol { get; set; } = "";
        public string Pattern { get; set; } = "";
        public int Quantity { get; set; }
    }
}
