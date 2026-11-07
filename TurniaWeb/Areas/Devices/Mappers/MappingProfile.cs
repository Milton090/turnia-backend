
using TurniaCore.Data.Entities.App.Queue;
using TurniaWeb.Areas.Devices.Dtos;
using TurniaCore.Data.Dto.Counters;
using TurniaCore.Data.Dto.Tickets;
using TurniaCore.Entities.Platform.User;
using TurniaCore.Data.Dto.User;

namespace TurniaWeb.Areas.Devices.Mappers;

public class MappingProfile : AutoMapper.Profile
{
    public MappingProfile()
    {
        CreateMap<Counter, Areas.Devices.Dtos.CounterDto>()
            .ForPath(c => c.Services, opt => opt.MapFrom(scr => scr.Services));

        CreateMap<Service, ServiceDto>();

        CreateMap<Service, TurniaCore.Data.Dto.Services.ServiceDto>();


        #region CounterInfo
        CreateMap<Counter, CounterInfoDto>();

        CreateMap<CounterInfoDto, Queue.Dtos.Counter.CounterInfoDto>()
            .ForMember(x => x.TicketInfo, opt => opt.MapFrom(src => src.TicketInfo))
            .ForMember(x => x.QueueUserProfile, opt => opt.MapFrom(src => src.QueueUserProfile));

        CreateMap<Ticket, TurniaCore.Data.Dto.Tickets.TicketInfoDto>();
        CreateMap<TurniaCore.Data.Dto.Tickets.TicketInfoDto, Queue.Dtos.Ticket.TicketInfoDto>();

        CreateMap<QueueUserProfile, UserInfoDto>();
        CreateMap<UserInfoDto, Queue.Dtos.User.UserInfoDto>();
        #endregion


        CreateMap<QueueUserProfile, UserDetailDto>();

        CreateMap<Ticket, TicketDto>();
    }
}
