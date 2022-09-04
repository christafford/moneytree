using AutoMapper;

namespace CStafford.Moneytree.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CryptoExchange.Net.CommonObjects.Kline, CStafford.Moneytree.Models.Tick>();
            CreateMap<CryptoExchange.Net.CommonObjects.Symbol, CStafford.Moneytree.Models.Symbol>();
        }
    }
}