using System.ComponentModel.DataAnnotations;

namespace CStafford.MoneyTree.Models
{
    public class Symbol
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? MinTradeQuantity { get; set; }
        public decimal? QuantityStep { get; set; }
        public decimal? PriceStep { get; set; }
        public int? QuantityDecimals { get; set; }
        public int? PriceDecimals { get; set; }
    }
}