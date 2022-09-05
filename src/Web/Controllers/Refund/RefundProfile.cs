using AutoMapper;

namespace Web.Controllers.Refund
{
    public class RefundProfile : Profile
    {
        public RefundProfile()
        {
            CreateMap<RefundModel, Application.Commands.RefundModel>();
        }
    }
}
