using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels.Filters
{
    public class UserFilter
    {
        public string Name { get; set; } = "";
        public string ProfileType { get; set; } = "";
        public string EntityId { get; set; } = "";

        public bool? ActiveStatus { get; set; } = null;
        public bool? AccountActivated { get; set; } = null;


        public List<UserRoleEnum> Roles { get; set; }
        public List<string> CustomFields { get; set; }
        public string CustomFieldValue { get; set; } = "";
    }

}
