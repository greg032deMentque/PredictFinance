using BackPredictFinance.ViewModels.CommonViewModels;

namespace BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels
{
	public class DocumentPaginateViewModel
	{
		public List<DocumentViewModel> Documents { get; set; }
		public int Count { get; set; }

		public DocumentPaginateViewModel(int count, List<DocumentViewModel> documents)
		{
			Count = count;
			Documents = documents;
		}
	}
}
