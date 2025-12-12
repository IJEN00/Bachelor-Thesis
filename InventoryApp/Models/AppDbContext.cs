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
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectItem> ProjectItems { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SupplierOffer> SupplierOffers { get; set; } = null!;
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Component)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Items)
                .WithOne(i => i.Project)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectItem>()
                .HasOne(i => i.Component)
                .WithMany(c => c.ProjectItems)
                .HasForeignKey(i => i.ComponentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Offers)
                .WithOne(o => o.Supplier)
                .HasForeignKey(o => o.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectItem>()
                .HasMany(pi => pi.SupplierOffers)
                .WithOne(o => o.ProjectItem)
                .HasForeignKey(o => o.ProjectItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Supplier>()
                .Property(s => s.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<SupplierOffer>()
                .Property(o => o.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("EUR");

            modelBuilder.Entity<SupplierOffer>()
                .Property(o => o.IsSelected)
                .HasDefaultValue(false);

            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(t => t.Project)
                .WithMany()
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}
