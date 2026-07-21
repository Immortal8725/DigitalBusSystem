namespace DigitalBusSystem.Models
{
    public class BusCard
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CardNumber { get; set; } = "";
        public string Status { get; set; } = "Active"; // Active, Blocked, Cancelled // Feature 3, 7
        public string QRCode { get; set; } = ""; // Feature 8
    }
}