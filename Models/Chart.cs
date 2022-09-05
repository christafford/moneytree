namespace CStafford.Moneytree.Models
{
    public class Chart
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public int MinutesForMarketAnalysis { get; set; }
        public int NumberOfHighestTradedForMarketAnalysis { get; set; }
        public int DaysSymbolsMustExist { get; set; }
        public decimal PercentagePlacementForSecurityPick { get; set; }
        public decimal ThreshholdToRiseForSell { get; set; }
        public decimal ThreshholdToDropForSell { get; set; }
    }
}