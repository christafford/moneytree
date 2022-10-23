using Microsoft.EntityFrameworkCore;

namespace CStafford.MoneyTree.Models;

[Index(nameof(SimulationId))]
public class SimulationLog
{
    [Dapper.Contrib.Extensions.Key]
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public int SimulationId { get; set; }
    public DateTime Time { get; set;}
    public string Action { get; set; }
    public string Message { get; set; }
}
