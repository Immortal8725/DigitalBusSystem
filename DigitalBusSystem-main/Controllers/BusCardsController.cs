using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalBusSystem.Models;

namespace DigitalBusSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusCardsController : ControllerBase
    {
        private static List<BusCard> cards = new List<BusCard>();

        // POST: api/BusCards/issue
        [HttpPost("issue")]
        public ActionResult<BusCard> IssueCard(int userId)
        {
            var card = new BusCard
            {
                Id = cards.Count + 1,
                UserId = userId,
                CardNumber = "CARD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                Status = "Active",
                QRCode = "QR-CARD-" + Guid.NewGuid().ToString() // Feature 8: Visual Card n QR Code
            };
            cards.Add(card);
            return Ok(card);
        }

        // POST: api/BusCards/block/1
        [HttpPost("block/{id}")]
        public ActionResult BlockCard(int id)
        {
            var card = cards.FirstOrDefault(c => c.Id == id);
            if (card == null) return NotFound();
            // Feature 3: Allows blockage of a lost card
            card.Status = "Blocked";
            return Ok("Card Blocked");
        }

        // POST: api/BusCards/cancel/1
        [HttpPost("cancel/{id}")]
        public ActionResult CancelCard(int id)
        {
            var card = cards.FirstOrDefault(c => c.Id == id);
            if (card == null) return NotFound();
            // Feature 7: Cancelation of a card
            card.Status = "Cancelled";
            return Ok("Card Cancelled");
        }

        [HttpGet("{id}")]
        public ActionResult<BusCard> GetCard(int id) => Ok(cards.FirstOrDefault(c => c.Id == id));
    }
}