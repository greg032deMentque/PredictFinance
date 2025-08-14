using AutoMapper;
using BackPredictFinance.Datas.Models;
using Microsoft.AspNetCore.Http;

namespace BackPredictFinance.ViewModels.CommonViewModels
{
    public class DocumentViewModel
	{
		public string? Id { get; set; }
        public string? FileName { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public IFormFile? File { get; set; }

        public DocumentViewModel() { }
	}

    public class DocumentViewModelProfile : Profile
    {
        public DocumentViewModelProfile()
        {
            CreateMap<Document, DocumentViewModel>();

            CreateMap<DocumentViewModel, Document>()
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.File != null ? src.File.FileName : src.FileName));
        }
    }

}
