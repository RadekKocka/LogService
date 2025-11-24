using SamkPoolOccupancyApi.Models;

namespace SamkPoolOccupancyApi.Repository;

public interface IOccupancyRepository
{
    public IEnumerable<LogEntry> GetAll();
}
