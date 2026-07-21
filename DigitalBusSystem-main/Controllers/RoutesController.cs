using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalBusSystem.Models;

namespace DigitalBusSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoutesController : ControllerBase
    {
        private static List<BusRoute> routes = new List<BusRoute>
        {
            new BusRoute{Id=1, RouteName="Soshanguve to Pretoria CBD", Stops="Sosh, Mabopane, Akasia, CBD", Price=25},
            new BusRoute{Id=2, RouteName="Centurion to Pretoria CBD", Stops="Centurion, Lynwood, CBD", Price=20}
        };

        // GET: api/Routes
        [HttpGet]
        public ActionResult<IEnumerable<BusRoute>> GetAllRoutes()
        {
            // Feature 5: A map for the routes
            return Ok(routes);
        }
    }
}