using BackPredictFinance.ViewModels.UserViewModels;

namespace BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels
{
	public class UserPaginateViewModel
    {
		public List<UserViewModel> Datas { get; set; }
		public int Count { get; set; }

		public UserPaginateViewModel(int count, List<UserViewModel> datas)
		{
			Count = count;
            Datas = datas;
		}
	}
}
