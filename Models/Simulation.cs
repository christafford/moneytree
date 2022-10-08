namespace CStafford.Moneytree.Models
{
    public class Simulation
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public int ChartId { get; set; }
        public DepositFrequencyEnum DepositFrequency { get; set; }
        public DateTime SimulationStart { get; set; }
        public DateTime SimulationEnd { get; set; }
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
            $"SimulationStart: {SimulationStart.ToString("g")}\n" +
            $"SimulationEnd: {SimulationEnd.ToString("g")}\n" +
            $"RunTimeStart: {RunTimeStart.ToString("g")}]\n" +
            $"RunTimeEnd: {RunTimeEnd.ToString("g")}\n" +
            $">>>>> ResultGainPercentage: {ResultGainPercentage}";
        }
    }
}