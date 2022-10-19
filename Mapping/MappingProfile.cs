using AutoMapper;

namespace CStafford.MoneyTree.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CryptoExchange.Net.CommonObjects.Kline, CStafford.MoneyTree.Models.Tick>();
            CreateMap<CryptoExchange.Net.CommonObjects.Symbol, CStafford.MoneyTree.Models.Symbol>();
        }
    }
}