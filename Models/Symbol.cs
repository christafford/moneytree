using System.ComponentModel.DataAnnotations;

namespace CStafford.Moneytree.Models
{
    public class Symbol
    {
        [Key]
        public string Name { get; set; }
        public decimal? MinTradeQuantity { get; set; }
        public decimal? QuantityStep { get; set; }
        public decimal? PriceStep { get; set; }
        public int? QuantityDecimals { get; set; }
        public int? PriceDecimals { get; set; }
    }
}