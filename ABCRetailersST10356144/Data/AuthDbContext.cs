using System.Collections.Generic;
using ABCRetailersST10356144.Models;
using Microsoft.EntityFrameworkCore;
namespace ABCRetailersST10356144.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<Cart> Cart => Set<Cart>();

        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ignore properties that EF Core cannot map
            modelBuilder.Entity<Order>().Ignore(o => o.ETag);
            modelBuilder.Entity<Order>().Ignore(o => o.Timestamp);
        }
    }
}
