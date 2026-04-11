using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using Salamaty.API.Models.HomeModels;
using Salamaty.API.Models.ProfileModels;
using SalamatyAPI.Models;

namespace SalamatyAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Banner> Banners { get; set; }
        public DbSet<MedicalProvider> MedicalProviders { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<MedicalProduct> MedicalProducts { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<ProductAlternative> ProductAlternatives { get; set; }
        public DbSet<Favorite> Favourites { get; set; }
        public DbSet<InsuranceProvider> InsuranceProviders { get; set; }
        public DbSet<InsuranceProfile> InsuranceProfiles { get; set; }
        public DbSet<InsuranceNetworkService> InsuranceNetworkServices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Favorite>().ToTable("Favourites");
            modelBuilder.Entity<Favorite>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(f => f.UserId);

            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.InsuranceProvider)
                .WithMany(i => i.InsuranceProfiles)
                .HasForeignKey(p => p.InsuranceProviderId);

            modelBuilder.Entity<ProductAlternative>()
                .HasKey(pa => new { pa.ProductId, pa.AlternativeProductId });

            modelBuilder.Entity<ProductAlternative>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.Alternatives)
                .HasForeignKey(pa => pa.ProductId) // تم التصحيح هنا من p لـ pa
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductAlternative>()
                .HasOne(pa => pa.AlternativeProduct)
                .WithMany(p => p.AlternativeTo)
                .HasForeignKey(pa => pa.AlternativeProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MedicalProduct>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InsuranceNetworkService>()
                .HasOne(s => s.InsuranceProvider)
                .WithMany(p => p.NetworkServices)
                .HasForeignKey(s => s.InsuranceProviderId);
        }
    }
}