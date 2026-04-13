using Group2.SWP391.SportsBicycles.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.DB
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Policy> Policies => Set<Policy>();
        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Bike> Bikes => Set<Bike>();
        public DbSet<Inspection> Inspections => Set<Inspection>();
        public DbSet<Media> Medias => Set<Media>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Review> Reviews => Set<Review>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Report> Reports => Set<Report>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --------- Soft delete global filter cho mọi entity kế thừa BaseEntity ---------
            ApplySoftDeleteQueryFilter(modelBuilder);

            // --------- Decimal precision (SQL Server) ---------
            modelBuilder.Entity<User>().Property(x => x.WalletBalance).HasPrecision(18, 2);

            modelBuilder.Entity<Bike>().Property(x => x.Price).HasPrecision(18, 2);
            modelBuilder.Entity<CartItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(x => x.LineTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Transaction>().Property(x => x.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<Policy>().Property(x => x.PercentOfSystem).HasPrecision(5, 2);
            modelBuilder.Entity<Policy>().Property(x => x.PercentOfSeller).HasPrecision(5, 2);

            // --------- User unique email ---------
            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            // --------- Listing (User 1-n Listing) ---------
            modelBuilder.Entity<Listing>()
                .HasOne(x => x.User)
                .WithMany(x => x.Listings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --------- Bike (Listing 1-n Bike) ---------
            modelBuilder.Entity<Bike>()
                .HasOne(x => x.Listing)
                .WithMany(x => x.Bikes)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            // --------- Bike 1-1 Inspection (Bike.InspectionId) ---------
            modelBuilder.Entity<Bike>()
                .HasOne(x => x.Inspection)
                .WithOne(x => x.Bike)
                .HasForeignKey<Bike>(x => x.InspectionId)
                .OnDelete(DeleteBehavior.SetNull);

            // --------- Inspection (User 1-n Inspection) ---------
            modelBuilder.Entity<Inspection>()
                .HasOne(x => x.User)
                .WithMany(x => x.Inspections)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --------- Media (Bike 1-n Media) ---------
            modelBuilder.Entity<Media>()
                .HasOne(x => x.Bike)
                .WithMany(x => x.Medias)
                .HasForeignKey(x => x.BikeId)
                .OnDelete(DeleteBehavior.Cascade);

            // --------- Cart (User 1-1 Cart) ---------
            modelBuilder.Entity<Cart>()
                .HasOne(x => x.User)
                .WithOne(x => x.Cart)
                .HasForeignKey<Cart>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // --------- CartItem (Cart 1-n CartItem, Bike 1-n CartItem) ---------
            modelBuilder.Entity<CartItem>()
                .HasOne(x => x.Cart)
                .WithMany(x => x.CartItems)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(x => x.Bike)
                .WithMany(x => x.CartItems)
                .HasForeignKey(x => x.BikeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasIndex(x => new { x.CartId, x.BikeId })
                .IsUnique();

            // --------- Wishlist (User n-n Bike) ---------
            modelBuilder.Entity<Wishlist>()
                .HasOne(x => x.User)
                .WithMany(x => x.Wishlists)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasOne(x => x.Bike)
                .WithMany(x => x.Wishlists)
                .HasForeignKey(x => x.BikeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasIndex(x => new { x.UserId, x.BikeId })
                .IsUnique();

            // --------- Order (User 1-n Order) ---------
            modelBuilder.Entity<Order>()
                .HasOne(x => x.User)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --------- OrderItem (Order 1-n, Bike 1-n) ---------
            modelBuilder.Entity<OrderItem>()
                .HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(x => x.Bike)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.BikeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(x => new { x.OrderId, x.BikeId })
                .IsUnique();

            // --------- Transaction (Order 1-1) ---------
            modelBuilder.Entity<Transaction>()
                .HasOne(x => x.Order)
                .WithOne(x => x.Transaction)
                .HasForeignKey<Transaction>(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // --------- Transaction (Policy 1-n) ---------
            modelBuilder.Entity<Transaction>()
                .HasOne(x => x.Policy)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasIndex(x => x.OrderCode)
                .IsUnique();

            // --------- Review (Order 1-1) ---------
            modelBuilder.Entity<Review>()
                .HasOne(x => x.Order)
                .WithOne(x => x.Review)
                .HasForeignKey<Review>(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => x.Token)
                .IsUnique();

            // --------- Report (User 1-n Report) ---------
            modelBuilder.Entity<Report>()
                .HasOne(x => x.User)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --------- Report (Order 1-n Report) ---------
            modelBuilder.Entity<Report>()
                .HasOne(x => x.Order)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => new { x.UserId, x.Revoked, x.ExpiredAt });
        }
            private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
        {
            var baseEntityType = typeof(BaseEntity);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.ClrType != null && baseEntityType.IsAssignableFrom(entityType.ClrType))
                {
                    // e => !e.IsDeleted
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var prop = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                    var condition = Expression.Equal(prop, Expression.Constant(false));
                    var lambda = Expression.Lambda(condition, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }
    }
    }

