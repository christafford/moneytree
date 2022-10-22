using AutoMapper;
using CStafford.MoneyTree.Configuration;

namespace CStafford.MoneyTree.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CryptoExchange.Net.CommonObjects.Kline, CStafford.MoneyTree.Models.Tick>()
                .ForMember( dest => dest.TickEpoch,
                            opt => opt.MapFrom(src => (src.OpenTime - Constants.Epoch).TotalMinutes))
                .ForMember( dest => dest.VolumeUsd,
                            opt => opt.MapFrom(src => src.Volume * src.ClosePrice));
            CreateMap<CryptoExchange.Net.CommonObjects.Symbol, CStafford.MoneyTree.Models.Symbol>();
        }
    }
}