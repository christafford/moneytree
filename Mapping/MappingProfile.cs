using AutoMapper;
using CryptoExchange.Net.CommonObjects;
using CStafford.Moneytree.Models;

namespace CStafford.Moneytree.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Kline, Tick>();
        }
    }
}