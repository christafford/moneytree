using System.ComponentModel.DataAnnotations;

namespace CStafford.Moneytree.Models
{
    public class Tick
    {
        [Key]
        public int Id { get; set; }
        public string SymbolName { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal? OpenPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal? ClosePrice { get; set; }
        public decimal? Volume { get; set; }
        public int PullDownId { get; set; }
    }
}