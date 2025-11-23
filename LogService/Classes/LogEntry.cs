using System.ComponentModel.DataAnnotations;

namespace LogService.Classes
{
    public class LogEntry
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int Occupancy { get; set; }
    }
}
