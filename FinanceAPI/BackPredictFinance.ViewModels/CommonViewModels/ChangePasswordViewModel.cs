using AutoMapper;
using Wagram.ONE.Data.Models;

namespace BackPredictFinance.ViewModels.CommonViewModels
{
    public class ChangePasswordViewModel
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }

    
}