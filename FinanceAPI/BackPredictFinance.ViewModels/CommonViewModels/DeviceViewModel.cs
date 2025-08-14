using AutoMapper;
using Wagram.ONE.Data.Models.user;

namespace BackPredictFinance.ViewModels.CommonViewModels
{
    public class DeviceViewModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string? MobileId { get; set; }
        public string? PushToken { get; set; }
        public bool IsIos { get; set; }
    }

    public class DeviceViewModelProfile : Profile
    {
        public DeviceViewModelProfile()
        {
            // Entity -> ViewModel
            CreateMap<Device, DeviceViewModel>();

            // ViewModel -> Entity
            CreateMap<DeviceViewModel, Device>();
        }
    }
}