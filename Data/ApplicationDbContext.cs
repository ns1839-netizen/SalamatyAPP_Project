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

        // --- جداول الـ Home والـ Providers ---
        public DbSet<Banner> Banners { get; set; }
        public DbSet<MedicalProvider> MedicalProviders { get; set; }
        public DbSet<Facility> Facilities { get; set; }

        // --- جداول المنتجات والروشتات (Nancy + Final) ---
        // السطر ده هيحل Error الـ Products اللي كان بيظهر في السيرفيس
        public DbSet<Product> Products { get; set; } 
        public DbSet<MedicalProduct> MedicalProducts { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<ProductAlternative> ProductAlternatives { get; set; }
        public DbSet<Favorite> Favourites { get; set; }

        // --- جداول التأمين ---
        public DbSet<InsuranceProvider> InsuranceProviders { get; set; }
        public DbSet<InsuranceProfile> InsuranceProfiles { get; set; }
        public DbSet<InsuranceNetworkService> InsuranceNetworkServices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. إعدادات المفضلات
            modelBuilder.Entity<Favorite>().ToTable("Favourites");
            modelBuilder.Entity<Favorite>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(f => f.UserId);

            // 2. إعدادات ملف التأمين والربط مع المستخدم
            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.InsuranceProvider)
                .WithMany(i => i.InsuranceProfiles)
                .HasForeignKey(p => p.InsuranceProviderId);

            // 3. إعدادات البدائل (ProductAlternatives)
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

            // 4. دقة سعر المنتج (Price Precision) لجميع الجداول
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MedicalProduct>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // 5. علاقات شبكة التأمين
            modelBuilder.Entity<InsuranceNetworkService>()
                .HasOne(s => s.InsuranceProvider)
                .WithMany(p => p.NetworkServices)
                .HasForeignKey(s => s.InsuranceProviderId);
        }
    }
}