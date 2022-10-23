using CStafford.MoneyTree.Configuration;

namespace CStafford.MoneyTree.Models
{
    public class Simulation
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public int ChartId { get; set; }
        public DepositFrequencyEnum DepositFrequency { get; set; }
        public int StartEpoch { get; set; }
        public int EndEpoch { get; set; }
        public DateTime RunTimeStart { get; set; }
        public DateTime RunTimeEnd { get; set; }
        public decimal ResultGainPercentage { get; set; }

        public enum DepositFrequencyEnum
        {
            Daily,
            Weekly,
            Monthly
        }

        public override string ToString()
        {
            return $"Id: {Id}\n" +
            $"ChartId: {ChartId}\n" +
            $"DepositFrequency: {DepositFrequency.ToString()}\n" +
            $"SimulationStart: {(Constants.Epoch.AddMinutes(StartEpoch)).ToString("g")}\n" +
            $"SimulationEnd: {(Constants.Epoch.AddMinutes(EndEpoch)).ToString("g")}\n" +
            $"RunTimeStart: {RunTimeStart.ToString("g")}]n" +
            $"RunTimeEnd: {RunTimeEnd.ToString("g")}\n" +
            $">>>>> ResultGainPercentage: {ResultGainPercentage}";
        }
    }
}