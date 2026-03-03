using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels
{
    public class PagedResultViewModel<T>
    {
        public List<T> Items { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
