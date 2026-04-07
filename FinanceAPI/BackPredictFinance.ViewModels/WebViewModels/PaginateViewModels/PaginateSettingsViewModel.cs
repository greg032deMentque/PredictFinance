using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels
{
    public class PaginateSettingsViewModel
    {
        public bool SortDirection { get; set; } = false;
        public string SortActive { get; set; } = "";
        public int PageSize { get; set; } = 5;
        public int PageIndex { get; set; } = 0;
        public string Filter { get; set; } = "";
    }
}
