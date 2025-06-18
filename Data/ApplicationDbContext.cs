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

        public DbSet<Parameter> Parameters { get; set; }
        public DbSet<Characteristic> Characteristics { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Important for Identity tables

            // Configure relationships

            // User -> SalesRepresentativeOrders (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.SalesRepresentativeOrders)
                .WithOne(o => o.SalesRepresentative)
                .HasForeignKey(o => o.SalesRepresentativeId)
                .OnDelete(DeleteBehavior.SetNull); // Changed to SetNull

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

            // Product to Parameters relationship
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Parameters)
                .WithOne(param => param.Product)
                .HasForeignKey(param => param.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product to Characteristics relationship
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Characteristics)
                .WithOne(c => c.Product)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product to Properties relationship
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Properties)
                .WithOne(prop => prop.Product)
                .HasForeignKey(prop => prop.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart and CartItem configurations

            // Client to Cart: One-to-One
            modelBuilder.Entity<Client>()
                .HasOne<Cart>()
                .WithOne(cart => cart.Client)
                .HasForeignKey<Cart>(cart => cart.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart to CartItems: One Cart has Many CartItems
            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem to Product: Many CartItems can reference one Product
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
