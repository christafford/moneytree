namespace CStafford.MoneyTree.Models
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
        public decimal ThresholdToRiseForSell { get; set; }
        public decimal ThresholdToDropForSell { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}\n" +
                $"MinutesForMarketAnalysis: {MinutesForMarketAnalysis}\n" +
                $"NumberOfHighestTradedForMarketAnalysis: {NumberOfHighestTradedForMarketAnalysis}\n" +
                $"DaysSymbolsMustExist: {DaysSymbolsMustExist}\n" +
                $"PercentagePlacementForSecurityPick: {PercentagePlacementForSecurityPick}\n" +
                $"ThresholdToRiseForSell: {ThresholdToRiseForSell}\n" +
                $"ThresholdToDropForSell: {ThresholdToDropForSell}";
        }
    }
}