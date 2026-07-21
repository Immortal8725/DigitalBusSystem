using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBusSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SupportController : ControllerBase
    {
        // POST: api/Support/contact
        [HttpPost("contact")]
        public ActionResult ContactSupport(int userId, string message)
        {
            // Feature 10: Customer care
            // In real app this would save to DB and email support team
            return Ok($"Message received from User {userId}. Support will contact you soon.");
        }
    }
}