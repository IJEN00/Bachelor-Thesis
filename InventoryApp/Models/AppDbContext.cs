using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Component> Components { get; set; } = null!;

        public DbSet<Document> Documents { get; set; } = null!;

        public DbSet<Location> Locations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Component)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
