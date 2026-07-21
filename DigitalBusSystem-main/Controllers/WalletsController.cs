using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalBusSystem.Models;

namespace DigitalBusSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletsController : ControllerBase
    {
        private readonly DigitalBusSystem.Data.AppDbContext _db;
        public WalletsController(DigitalBusSystem.Data.AppDbContext db) => _db = db;

        // POST: api/Wallets/topup
        [HttpPost("topup")]
        public ActionResult<Wallet> TopUpWallet(int userId, decimal amount)
        {
            var wallet = _db.Wallets.FirstOrDefault(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0 };
                _db.Wallets.Add(wallet);
            }
            wallet.Balance += amount; // Add money
            _db.SaveChanges();
            return Ok(wallet);
        }

        // GET: api/Wallets/1
        [HttpGet("{userId}")]
        public ActionResult<Wallet> GetWallet(int userId)
        {
            var wallet = _db.Wallets.FirstOrDefault(w => w.UserId == userId);
            if (wallet == null) return NotFound("Wallet not found");
            return Ok(wallet);
        }
    }
}