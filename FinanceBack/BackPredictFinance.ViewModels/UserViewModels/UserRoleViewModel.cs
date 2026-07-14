using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.UserViewModels
{

    public class UserRoleViewModel
    {
        public string RoleId { get; set; } = string.Empty;

        /// <summary>
        /// Valeur enum associée, pour un code plus sûr.
        /// </summary>
        public UserRoleEnum UserRole { get; set; }

        /// <summary>
        /// Pour mettre le nom du role en français ou en anglais
        /// </summary>
        public string RoleName { get; set; } = string.Empty;

    }

}
