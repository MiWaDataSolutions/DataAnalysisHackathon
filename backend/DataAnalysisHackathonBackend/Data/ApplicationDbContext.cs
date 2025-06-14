using Microsoft.EntityFrameworkCore;
using DataAnalysisHackathonBackend.Models;

namespace DataAnalysisHackathonBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<DataSession> DataSessions { get; set; }

        // Optional: Override OnModelCreating for further configuration if needed in the future
        // protected override void OnModelCreating(ModelBuilder modelBuilder)
        // {
        //     base.OnModelCreating(modelBuilder);
        //     // Example: Configure GoogleId as unique index if not already handled by [Key] for specific DB needs
        //     // modelBuilder.Entity<User>()
        //     //     .HasIndex(u => u.GoogleId)
        //     //     .IsUnique();
        // }
    }
}
