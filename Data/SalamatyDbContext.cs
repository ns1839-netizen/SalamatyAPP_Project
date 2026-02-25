using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Models;

namespace SalamatyAPI.Data
{
    public class SalamatyDbContext : DbContext
    {
        public SalamatyDbContext(DbContextOptions<SalamatyDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();

        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductAlternative> ProductAlternatives => Set<ProductAlternative>();
        public DbSet<Favorite> Favorites => Set<Favorite>();

        public DbSet<InsuranceProvider> InsuranceProviders => Set<InsuranceProvider>();
        public DbSet<InsuranceProfile> InsuranceProfiles => Set<InsuranceProfile>();
        public DbSet<InsuranceNetworkService> InsuranceNetworkServices => Set<InsuranceNetworkService>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ProductAlternatives composite key + relations
            modelBuilder.Entity<ProductAlternative>()
                .HasKey(pa => new { pa.ProductId, pa.AlternativeProductId });

            modelBuilder.Entity<ProductAlternative>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.Alternatives)
                .HasForeignKey(pa => pa.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductAlternative>()
                .HasOne(pa => pa.AlternativeProduct)
                .WithMany(p => p.AlternativeTo)
                .HasForeignKey(pa => pa.AlternativeProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Price precision
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Insurance relations
            modelBuilder.Entity<InsuranceNetworkService>()
                .HasOne(s => s.InsuranceProvider)
                .WithMany(p => p.NetworkServices)
                .HasForeignKey(s => s.InsuranceProviderId);

            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.InsuranceProvider)
                .WithMany(i => i.InsuranceProfiles)
                .HasForeignKey(p => p.InsuranceProviderId);

            // User relation (InsuranceProfile.UserId -> User.Id) is by convention
        }
    }
}

