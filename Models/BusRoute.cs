namespace DigitalBusSystem.Models
{
    public class BusRoute
    {
        public int Id { get; set; }
        public string RouteName { get; set; } = ""; // e.g. "Soshanguve to Pretoria CBD"
        public string Stops { get; set; } = ""; // Feature 5: A map for the routes
        public decimal Price { get; set; }
    }
}