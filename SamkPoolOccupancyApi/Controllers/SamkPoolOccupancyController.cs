using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamkPoolOccupancyApi.Models;
using SamkPoolOccupancyApi.Repository;

namespace SamkPoolOccupancyApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SamkPoolOccupancyController : ControllerBase
    {
        private readonly IOccupancyRepository _occupancyRepository;
        public SamkPoolOccupancyController(IOccupancyRepository occupancyRepository)
        {
            _occupancyRepository = occupancyRepository;
        }

        [HttpGet]
        public ActionResult<List<LogEntry>> Get()
        {
            return Ok(_occupancyRepository.GetAll().ToList());
        }
    }
}
