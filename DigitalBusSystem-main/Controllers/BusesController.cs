using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalBusSystem.Models;

namespace DigitalBusSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusesController : ControllerBase
    {
        private static List<Bus> buses = new List<Bus>();

        [HttpGet]
        public ActionResult<IEnumerable<Bus>> GetBuses()
        {
            return Ok(buses);
        }

        [HttpPost]
        public ActionResult<Bus> AddBus(Bus bus)
        {
            bus.Id = buses.Count + 1;
            buses.Add(bus);
            return CreatedAtAction(nameof(GetBuses), new { id = bus.Id }, bus);
        }
    }
}