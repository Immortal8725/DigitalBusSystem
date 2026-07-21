namespace DigitalBusSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Language { get; set; } = "en";
        public string SecurityQuestion { get; set; } = "";
        public string SecurityAnswer { get; set; } = "";
        public bool IsLoggedIn { get; set; } = false;
        public bool IsAdmin { get; set; } = false;

        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    }
}