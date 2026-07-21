namespace DigitalBusSystem.Models
{
    public class Bus
    {
        public int Id { get; set; }
        public string BusNumber { get; set; } = "";
        public string Route { get; set; } = "";
        public int Capacity { get; set; }
    }
}