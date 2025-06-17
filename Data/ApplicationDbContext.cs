using EventsService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventsService.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, int> // Specify int for key types
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Checklist> Checklists { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Important for Identity tables

            // Configure relationships

            // User -> SalesRepresentativeOrders (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.SalesRepresentativeOrders)
                .WithOne(o => o.SalesRepresentative)
                .HasForeignKey(o => o.SalesRepresentativeId)
                .OnDelete(DeleteBehavior.Restrict); // Or .SetNull if preferred

            // Client -> Orders (One-to-Many)
            modelBuilder.Entity<Client>()
                .HasMany(c => c.Orders)
                .WithOne(o => o.Client)
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Cascade); // Or .Restrict based on requirements

            // Order -> OrderItems (One-to-Many)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product -> OrderItems (One-to-Many)
            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting product if it's in an order item

            // Order -> Checklists (One-to-Many)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Checklists)
                .WithOne(cl => cl.Order)
                .HasForeignKey(cl => cl.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Checklist -> ChecklistItems (One-to-Many)
            modelBuilder.Entity<Checklist>()
                .HasMany(cl => cl.ChecklistItems)
                .WithOne(cli => cli.Checklist)
                .HasForeignKey(cli => cli.ChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Client (One-to-One)
            // This relationship is already defined by ForeignKey in Client model.
            // If UserId in Client is null, then the client is not linked to a system user.
             modelBuilder.Entity<Client>()
                .HasOne(c => c.User)
                .WithOne(u => u.Client)
                .HasForeignKey<Client>(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull); // If User is deleted, set Client.UserId to null

        }
    }
}
