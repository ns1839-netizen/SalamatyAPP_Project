using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using Salamaty.API.Models.HomeModels;
using Salamaty.API.Models.ProfileModels;

namespace SalamatyAPI.Data
{
    // الوراثة من IdentityDbContext أساسية لدعم AspNetUsers
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // جداول الـ Home والـ Providers
        public DbSet<Banner> Banners { get; set; }
        public DbSet<MedicalProvider> MedicalProviders { get; set; }

        public DbSet<Facility> Facilities { get; set; }
        // نقل الجداول من SalamatyDbContext
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAlternative> ProductAlternatives { get; set; }
        public DbSet<Favorite> Favourites { get; set; }
        public DbSet<InsuranceProvider> InsuranceProviders { get; set; }
        public DbSet<InsuranceProfile> InsuranceProfiles { get; set; }
        public DbSet<InsuranceNetworkService> InsuranceNetworkServices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // مهم جداً جداً



            // إجبار جدول الـ InsuranceProfiles على استخدام AspNetUsers كـ Foreign Key
            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);
            // 1. إعدادات البدائل (ProductAlternatives)
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

            // 2. دقة سعر المنتج (Price Precision)
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // 3. علاقات التأمين (Insurance Relations)
            modelBuilder.Entity<InsuranceNetworkService>()
                .HasOne(s => s.InsuranceProvider)
                .WithMany(p => p.NetworkServices)
                .HasForeignKey(s => s.InsuranceProviderId);

            modelBuilder.Entity<InsuranceProfile>()
                .HasOne(p => p.InsuranceProvider)
                .WithMany(i => i.InsuranceProfiles)
                .HasForeignKey(p => p.InsuranceProviderId);

            // 4. الربط بين المفضلات والمستخدم (ApplicationUser)
            // هذا السطر هو الذي سيحل مشكلة الـ 500 Error
            modelBuilder.Entity<Favorite>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(f => f.UserId);
        }
    }
}

