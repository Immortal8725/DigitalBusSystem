using Microsoft.EntityFrameworkCore;
using DigitalBusSystem.Models;

namespace DigitalBusSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;
        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<BusCard> BusCards { get; set; } = null!;
        public DbSet<Bus> Buses { get; set; } = null!;
        public DbSet<BusRoute> BusRoutes { get; set; } = null!;
    }
}