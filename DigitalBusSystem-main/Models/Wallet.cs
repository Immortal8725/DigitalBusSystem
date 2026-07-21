namespace DigitalBusSystem.Models
{
    public class Wallet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Balance { get; set; } = 0; // Step 2: Top-Up
    }
}