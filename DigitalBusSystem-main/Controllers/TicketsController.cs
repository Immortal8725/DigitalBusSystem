using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalBusSystem.Models;

namespace DigitalBusSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly DigitalBusSystem.Data.AppDbContext _db;
        public TicketsController(DigitalBusSystem.Data.AppDbContext db) => _db = db;

        // POST: api/Tickets/buy
        [HttpPost("buy")]
        public ActionResult<Ticket> BuyTicket(int buyerUserId, int routeId, decimal price, string passengerPhone = "")
        {
            // 1. Deduct from wallet - Step 3
            var wallet = _db.Wallets.FirstOrDefault(w => w.UserId == buyerUserId);
            if (wallet == null || wallet.Balance < price) return BadRequest("Not enough balance");
            wallet.Balance -= price;
            _db.SaveChanges();

            // 2. Generate one-time ticket code - Step 4
            var ticket = new Ticket
            {
                BuyerUserId = buyerUserId,
                PassengerPhone = string.IsNullOrEmpty(passengerPhone) ? "Self" : passengerPhone,
                RouteId = routeId,
                Price = price,
                TicketCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                QRCode = "QR-" + Guid.NewGuid().ToString(),
                Status = "Active",
                CreatedAt = DateTime.Now
            };
            _db.Tickets.Add(ticket);
            _db.SaveChanges();
            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        // POST: api/Tickets/validate
        [HttpPost("validate/{code}")]
        public ActionResult ValidateTicket(string code)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.TicketCode == code);
            if (ticket == null) return NotFound("Invalid ticket");
            if (ticket.Status == "Used") return BadRequest("Ticket already used");

            ticket.Status = "Used"; // deactivate
            _db.SaveChanges();
            return Ok("Ticket Valid. Board the bus.");
        }

        [HttpGet("{id}")]
        public ActionResult<Ticket> GetTicket(int id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();
            return Ok(ticket);
        }
    }
}