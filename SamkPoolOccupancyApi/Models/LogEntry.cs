using System.ComponentModel.DataAnnotations;

namespace SamkPoolOccupancyApi.Models
{
    public class LogEntry
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int Occupancy { get; set; }
    }
}
