namespace DigitalBusSystem.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string TicketCode { get; set; } = ""; // Step 4: One-time code
        public int? BuyerUserId { get; set; } // Who paid
        public string PassengerPhone { get; set; } = ""; // Step 3 Path B: Buy for someone
        public int RouteId { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = "Active"; // Active, Used, Cancelled // Step 8
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string QRCode { get; set; } = ""; // Feature 8: Visual Card n QR Code
    }
}